using System.Globalization;

using App.Web.Features.Upload.Api;
using App.Web.Features.Upload.ControlPlane;
using App.Web.Features.Upload.Models;
using App.Web.Features.Upload.Presentation;
using App.Web.Features.Upload.Services;
using App.Web.Features.Upload.SiteGateway;

using BuildingBlocks.Contracts.VideoUpload;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace App.Web.Features.Upload.Pages;

public partial class UploadPage
{
    private const long MaxSelectedVideoFileSizeBytes = 5L * 1024 * 1024 * 1024;

    private static readonly Action<ILogger, string, Exception?> LogUnsupportedPreUploadDecision =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2001, nameof(LogUnsupportedPreUploadDecision)),
            "Unsupported pre-upload decision returned by backend: {Decision}");

    private static readonly Action<ILogger, string, Exception?> LogDirectSiteUploadAdapterDidNotComplete =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2003, nameof(LogDirectSiteUploadAdapterDidNotComplete)),
            "Direct site upload adapter did not complete: {Message}");

    private static readonly Action<ILogger, string, Exception?> LogUnsupportedUploadReceiptStatus =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2005, nameof(LogUnsupportedUploadReceiptStatus)),
            "Unsupported upload receipt status returned by backend: {Status}");

    private static readonly Action<ILogger, Exception?> LogUploadFlowFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2007, nameof(LogUploadFlowFailed)),
            "Upload flow failed.");

    private static readonly Action<ILogger, Exception?> LogControlPlaneSignInFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2101, nameof(LogControlPlaneSignInFailed)),
            "Upload control plane sign-in failed.");

    private static readonly Action<ILogger, Exception?> LogControlPlaneGroupTreeLoadFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2102, nameof(LogControlPlaneGroupTreeLoadFailed)),
            "Upload control plane group tree load failed.");

    private readonly UploadPreCheckFormModel _form = new()
    {
        ContentType = "video/mp4",
        CapturedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
    };

    private readonly UploadControlPlaneSignInFormModel _signInForm = new();

    private bool _isSubmitting;
    private bool _isControlPlaneSubmitting;
    private string? _statusMessage;
    private string? _errorMessage;
    private string? _controlPlaneErrorMessage;
    private UploadSiteConnectionPhase _phase = UploadSiteConnectionPhase.Idle;
    private VideoPreUploadCheckResponseDto? _preUploadResponse;
    private UploadDecisionPresentationModel? _decisionModel;
    private DirectSiteVideoUploadResult? _directUploadResult;
    private UploadReceiptFormModel? _receiptForm;
    private VideoUploadReceiptResponseDto? _receiptResponse;
    private UploadReceiptPresentationModel? _receiptModel;
    private UploadControlPlaneSanitizedSession? _sessionSummary;
    private IReadOnlyList<UploadControlPlaneGroupNode> _groupNodes = Array.Empty<UploadControlPlaneGroupNode>();
    private UploadControlPlaneGroupNode? _selectedGroupNode;
    private IBrowserFile? _selectedBrowserFile;
    private UploadSelectedVideoFileMetadata? _selectedFileMetadata;

    [Inject]
    public IVideoUploadApi VideoUploadApi { get; set; } = default!;

    [Inject]
    public IDirectSiteVideoUploadAdapter DirectSiteVideoUploadAdapter { get; set; } = default!;

    [Inject]
    public IUploadControlPlaneApi UploadControlPlaneApi { get; set; } = default!;

    [Inject]
    public IUploadControlPlaneSessionStore SessionStore { get; set; } = default!;

    [Inject]
    public IUploadOnlineStatusProvider OnlineStatusProvider { get; set; } = default!;

    [Inject]
    public ILogger<UploadPage> Logger { get; set; } = default!;

    private UploadSiteConnectionPhaseModel CurrentPhaseModel =>
        UploadSiteConnectionPhasePresentation.FromPhase(_phase);

    private bool IsBusy =>
        _isSubmitting ||
        _isControlPlaneSubmitting ||
        _phase is UploadSiteConnectionPhase.SigningIn
            or UploadSiteConnectionPhase.LoadingGroups
            or UploadSiteConnectionPhase.HashingFile
            or UploadSiteConnectionPhase.Precheck
            or UploadSiteConnectionPhase.LocalSiteUpload
            or UploadSiteConnectionPhase.ReceiptSubmit;

    private bool CanLoadGroups =>
        UploadFeatureGate.IsEnabled &&
        !IsBusy &&
        _sessionSummary is not null;

    private bool CanStartUploadFlow =>
        UploadFeatureGate.IsEnabled &&
        !IsBusy &&
        _sessionSummary?.UserId is not null &&
        _selectedGroupNode?.Id.HasValue == true &&
        _selectedFileMetadata is not null &&
        !string.IsNullOrWhiteSpace(_form.BusinessObjectKey);

    protected override async Task OnInitializedAsync()
    {
        if (!UploadFeatureGate.IsEnabled)
        {
            return;
        }

        await LoadStoredSessionSummaryAsync();
    }

    private async Task SignInControlPlaneAsync()
    {
        if (!EnsureFeatureEnabled() || !await EnsureOnlineAsync())
        {
            return;
        }

        _isControlPlaneSubmitting = true;
        SetPhase(UploadSiteConnectionPhase.SigningIn, "Signing in to the control plane.");
        _controlPlaneErrorMessage = null;

        try
        {
            UploadControlPlaneSignInRequest request =
                UploadControlPlaneSessionFactory.CreateSignInRequest(_signInForm);

            UploadControlPlaneSignInResponse response =
                await UploadControlPlaneApi.SignInAsync(request);

            UploadControlPlaneSession session = UploadControlPlaneSessionFactory.CreateSession(
                response,
                DateTimeOffset.UtcNow);

            await SessionStore.SetAsync(session);
            _sessionSummary = session.ToSanitized();
            ApplySessionToForm(_sessionSummary);
            SetPhase(UploadSiteConnectionPhase.Idle, "Control plane session ready. Credential values are hidden.");
        }
        catch (Exception exception)
        {
            LogControlPlaneSignInFailed(Logger, exception);
            _controlPlaneErrorMessage = SafeMessage(exception);
            SetPhase(UploadSiteConnectionPhase.Failed, "Control plane sign-in failed.");
        }
        finally
        {
            _signInForm.Password = string.Empty;
            _isControlPlaneSubmitting = false;
        }
    }

    private async Task LoadGroupTreeNodesAsync()
    {
        if (!EnsureFeatureEnabled() || !await EnsureOnlineAsync())
        {
            return;
        }

        _isControlPlaneSubmitting = true;
        SetPhase(UploadSiteConnectionPhase.LoadingGroups, "Loading group tree.");
        _controlPlaneErrorMessage = null;

        try
        {
            UploadControlPlaneSession? session = await SessionStore.GetAsync();

            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                _controlPlaneErrorMessage = "Control plane session is required before group tree load.";
                SetPhase(UploadSiteConnectionPhase.Failed, "Group tree load stopped.");
                return;
            }

            _groupNodes = await UploadControlPlaneApi.GetGroupTreeNodesAsync(session.AccessToken);
            SetPhase(UploadSiteConnectionPhase.Idle, $"{_groupNodes.Count} group tree nodes loaded.");
        }
        catch (Exception exception)
        {
            LogControlPlaneGroupTreeLoadFailed(Logger, exception);
            _controlPlaneErrorMessage = SafeMessage(exception);
            SetPhase(UploadSiteConnectionPhase.Failed, "Group tree load failed.");
        }
        finally
        {
            _isControlPlaneSubmitting = false;
        }
    }

    private void SelectGroupNode(UploadControlPlaneGroupNode node)
    {
        if (!UploadGroupNodeSelection.CanSelect(node))
        {
            _controlPlaneErrorMessage = "Selected group node is not selectable.";
            return;
        }

        _selectedGroupNode = node;
        _form.GroupNodeId = node.Id!.Value.ToString();
        _controlPlaneErrorMessage = null;
        ResetResult();
        SetPhase(UploadSiteConnectionPhase.Idle, "Group selected.");
    }

    private bool CanSelectGroupNode(UploadControlPlaneGroupNode node) =>
        !IsBusy && UploadGroupNodeSelection.CanSelect(node);

    private async Task HandleVideoFileSelectedAsync(InputFileChangeEventArgs args)
    {
        if (!EnsureFeatureEnabled())
        {
            return;
        }

        ResetResult();
        _selectedBrowserFile = null;
        _selectedFileMetadata = null;

        if (args.FileCount != 1)
        {
            _errorMessage = "Select exactly one video file.";
            SetPhase(UploadSiteConnectionPhase.Failed, "Video file selection failed.");
            return;
        }

        IBrowserFile file = args.File;

        if (file.Size > MaxSelectedVideoFileSizeBytes)
        {
            _errorMessage = "Selected video file exceeds the allowed baseline size.";
            SetPhase(UploadSiteConnectionPhase.Failed, "Video file selection failed.");
            return;
        }

        _isSubmitting = true;
        SetPhase(UploadSiteConnectionPhase.HashingFile, "Hashing selected video file.");

        try
        {
            await using Stream stream = file.OpenReadStream(MaxSelectedVideoFileSizeBytes);
            string sha256 = await UploadSelectedVideoFileMetadataFactory.ComputeSha256HexAsync(stream);

            UploadSelectedVideoFileMetadata metadata = UploadSelectedVideoFileMetadataFactory.Create(
                file.Name,
                file.Size,
                file.ContentType,
                sha256,
                file.LastModified);

            _selectedBrowserFile = file;
            _selectedFileMetadata = metadata;
            UploadPreCheckFormAutoFill.ApplySelectedFile(_form, metadata);
            SetPhase(UploadSiteConnectionPhase.Idle, "Video file metadata ready.");
        }
        catch (Exception exception)
        {
            _errorMessage = SafeMessage(exception);
            SetPhase(UploadSiteConnectionPhase.Failed, "Video file processing failed.");
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task ClearControlPlaneSessionAsync()
    {
        await SessionStore.ClearAsync();

        _sessionSummary = null;
        _groupNodes = Array.Empty<UploadControlPlaneGroupNode>();
        _selectedGroupNode = null;
        _form.UserId = string.Empty;
        _form.GroupNodeId = string.Empty;
        _controlPlaneErrorMessage = null;
        _signInForm.Password = string.Empty;
        ResetResult();
        SetPhase(UploadSiteConnectionPhase.Idle, "Control plane session cleared.");
    }

    private async Task RunUploadFlowAsync()
    {
        if (!EnsureFeatureEnabled() || !await EnsureOnlineAsync() || !EnsureUploadInputReady())
        {
            return;
        }

        _isSubmitting = true;
        ResetResult();

        try
        {
            SetPhase(UploadSiteConnectionPhase.Precheck, "Running pre-upload check.");
            VideoPreUploadCheckRequestDto preCheckRequest = UploadPreCheckRequestFactory.Create(_form);
            VideoPreUploadCheckResponseDto preCheckResponse =
                await VideoUploadApi.CheckPreUploadAsync(preCheckRequest);

            _preUploadResponse = preCheckResponse;
            _decisionModel = UploadDecisionPresentation.FromDecision(preCheckResponse.Decision);

            if (_decisionModel.IsUnsupported)
            {
                LogUnsupportedPreUploadDecision(Logger, preCheckResponse.Decision, null);
            }

            if (!_decisionModel.CanContinue)
            {
                SetPhase(UploadSiteConnectionPhase.Blocked, "Pre-upload decision stopped the upload flow.");
                return;
            }

            if (!await EnsureOnlineAsync())
            {
                return;
            }

            SetPhase(UploadSiteConnectionPhase.LocalSiteUpload, "Running local direct-site upload boundary.");
            DirectSiteVideoUploadResult uploadResult = await RunDirectSiteUploadBoundaryAsync();

            _directUploadResult = uploadResult;

            if (!uploadResult.Succeeded)
            {
                LogDirectSiteUploadAdapterDidNotComplete(Logger, uploadResult.Message, null);
                _errorMessage = UploadControlPlaneErrorRedactor.Redact(uploadResult.Message);
                SetPhase(UploadSiteConnectionPhase.Failed, "Local direct-site upload boundary failed.");
                return;
            }

            _receiptForm = CreateReceiptForm(uploadResult);

            SetPhase(UploadSiteConnectionPhase.ReceiptSubmit, "Submitting upload receipt.");
            VideoUploadReceiptRequestDto receiptRequest = UploadReceiptRequestFactory.Create(_receiptForm);
            VideoUploadReceiptResponseDto receiptResponse =
                await VideoUploadApi.SubmitUploadReceiptAsync(receiptRequest);

            _receiptResponse = receiptResponse;
            _receiptModel = UploadReceiptPresentation.FromStatus(receiptResponse.Status);

            if (_receiptModel.IsUnsupported)
            {
                LogUnsupportedUploadReceiptStatus(Logger, receiptResponse.Status, null);
            }

            SetPhase(
                _receiptModel.IsAccepted
                    ? UploadSiteConnectionPhase.Success
                    : UploadSiteConnectionPhase.Failed,
                _receiptModel.IsAccepted
                    ? "Upload receipt confirmed."
                    : "Upload receipt was not accepted.");
        }
        catch (Exception exception)
        {
            LogUploadFlowFailed(Logger, exception);
            _errorMessage = SafeMessage(exception);
            SetPhase(UploadSiteConnectionPhase.Failed, "Upload flow failed.");
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task<DirectSiteVideoUploadResult> RunDirectSiteUploadBoundaryAsync()
    {
        if (_preUploadResponse is null)
        {
            throw new InvalidOperationException("PreUploadCheck must complete before direct site upload boundary can run.");
        }

        if (_selectedBrowserFile is null)
        {
            throw new InvalidOperationException("Selected video file is required before direct site upload boundary can run.");
        }

        DirectSiteVideoUploadDraft draft = new(
            PreUploadCheckId: _preUploadResponse.PreUploadCheckId,
            FileName: _form.FileName,
            SizeBytes: _form.SizeBytes,
            ByteSha256: _form.ByteSha256,
            ContentType: _form.ContentType);

        await using Stream content = _selectedBrowserFile.OpenReadStream(MaxSelectedVideoFileSizeBytes);

        return await DirectSiteVideoUploadAdapter.UploadAsync(draft, content);
    }

    private UploadReceiptFormModel CreateReceiptForm(DirectSiteVideoUploadResult uploadResult)
    {
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        string idempotencyKey = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        return new UploadReceiptFormModel
        {
            PreUploadCheckId = _preUploadResponse?.PreUploadCheckId.ToString() ?? string.Empty,
            IdempotencyKey = idempotencyKey,
            UserId = _form.UserId,
            DeviceId = _form.DeviceId,
            GroupNodeId = _form.GroupNodeId,
            BusinessObjectKey = _form.BusinessObjectKey,
            ExternalVideoId = uploadResult.ExternalVideoId ?? "site-video-not-configured",
            FileName = _form.FileName,
            ContentType = _form.ContentType,
            StorageKey = uploadResult.StorageKey ?? "videos/site-video-not-configured.mp4",
            SiteStatus = uploadResult.SiteStatus ?? LocalStubDirectSiteVideoUploadAdapter.LocalStubSiteStatus,
            SizeBytes = _form.SizeBytes,
            ByteSha256 = _form.ByteSha256,
            UploadedAtUtc = now
        };
    }

    private void ResetResult()
    {
        _errorMessage = null;
        _preUploadResponse = null;
        _decisionModel = null;
        _directUploadResult = null;
        _receiptForm = null;
        _receiptResponse = null;
        _receiptModel = null;

        if (_phase is UploadSiteConnectionPhase.Success
            or UploadSiteConnectionPhase.Blocked
            or UploadSiteConnectionPhase.Failed)
        {
            SetPhase(UploadSiteConnectionPhase.Idle, "Ready");
        }
    }

    private async Task LoadStoredSessionSummaryAsync()
    {
        UploadControlPlaneSession? session = await SessionStore.GetAsync();
        _sessionSummary = session?.ToSanitized();
        ApplySessionToForm(_sessionSummary);
    }

    private bool EnsureFeatureEnabled()
    {
        if (UploadFeatureGate.IsEnabled)
        {
            return true;
        }

        _errorMessage = "Upload UI baseline is disabled by feature flag.";
        SetPhase(UploadSiteConnectionPhase.Failed, "Feature flag is off.");
        return false;
    }

    private async Task<bool> EnsureOnlineAsync()
    {
        bool isOnline = await OnlineStatusProvider.IsOnlineAsync();

        if (isOnline)
        {
            return true;
        }

        _errorMessage = "Online connection is required for this upload baseline.";
        SetPhase(UploadSiteConnectionPhase.Failed, "Browser is offline or online status is unavailable.");
        return false;
    }

    private bool EnsureUploadInputReady()
    {
        if (_sessionSummary?.UserId is null)
        {
            _errorMessage = "Control plane session with user id is required.";
            SetPhase(UploadSiteConnectionPhase.Failed, "Upload input is incomplete.");
            return false;
        }

        if (_selectedGroupNode?.Id is null)
        {
            _errorMessage = "Selectable group is required.";
            SetPhase(UploadSiteConnectionPhase.Failed, "Upload input is incomplete.");
            return false;
        }

        if (_selectedFileMetadata is null || _selectedBrowserFile is null)
        {
            _errorMessage = "Selected video file is required.";
            SetPhase(UploadSiteConnectionPhase.Failed, "Upload input is incomplete.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_form.BusinessObjectKey))
        {
            _errorMessage = "Business object key is required.";
            SetPhase(UploadSiteConnectionPhase.Failed, "Upload input is incomplete.");
            return false;
        }

        ApplySessionToForm(_sessionSummary);
        _form.GroupNodeId = _selectedGroupNode.Id.Value.ToString();

        return true;
    }

    private void ApplySessionToForm(UploadControlPlaneSanitizedSession? session)
    {
        if (session?.UserId is not null)
        {
            _form.UserId = session.UserId.Value.ToString();
        }
    }

    private void SetPhase(UploadSiteConnectionPhase phase, string? statusMessage)
    {
        _phase = phase;
        _statusMessage = statusMessage;
    }

    private static string SafeMessage(Exception exception) =>
        UploadControlPlaneErrorRedactor.Redact(exception.Message);

    private static string AlertClass(string tone) => tone switch
    {
        "success" => "alert alert-success mt-3",
        "warning" => "alert alert-warning mt-3",
        "danger" => "alert alert-danger mt-3",
        "info" => "alert alert-info mt-3",
        _ => "alert alert-secondary mt-3"
    };

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Not set" : value;
}