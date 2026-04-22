using System.Globalization;

using App.Web.Features.Upload.Api;
using App.Web.Features.Upload.ControlPlane;
using App.Web.Features.Upload.Models;
using App.Web.Features.Upload.Presentation;
using App.Web.Features.Upload.SiteGateway;

using BuildingBlocks.Contracts.VideoUpload;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace App.Web.Features.Upload.Pages;

public partial class UploadPage
{
    private static readonly Action<ILogger, string, Exception?> LogUnsupportedPreUploadDecision =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2001, nameof(LogUnsupportedPreUploadDecision)),
            "Unsupported pre-upload decision returned by backend: {Decision}");

    private static readonly Action<ILogger, Exception?> LogUploadScreenPreUploadCheckFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2002, nameof(LogUploadScreenPreUploadCheckFailed)),
            "Upload screen pre-upload check failed.");

    private static readonly Action<ILogger, string, Exception?> LogDirectSiteUploadAdapterDidNotComplete =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2003, nameof(LogDirectSiteUploadAdapterDidNotComplete)),
            "Direct site upload adapter did not complete: {Message}");

    private static readonly Action<ILogger, Exception?> LogDirectSiteUploadAdapterBoundaryFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2004, nameof(LogDirectSiteUploadAdapterBoundaryFailed)),
            "Direct site upload adapter boundary failed.");

    private static readonly Action<ILogger, string, Exception?> LogUnsupportedUploadReceiptStatus =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2005, nameof(LogUnsupportedUploadReceiptStatus)),
            "Unsupported upload receipt status returned by backend: {Status}");

    private static readonly Action<ILogger, Exception?> LogUploadReceiptSubmissionFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2006, nameof(LogUploadReceiptSubmissionFailed)),
            "Upload receipt submission failed.");

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
        UserId = "11111111-1111-1111-1111-111111111111",
        DeviceId = "22222222-2222-2222-2222-222222222222",
        GroupNodeId = "33333333-3333-3333-3333-333333333333",
        BusinessObjectKey = "demo-business-object",
        FileName = "sample.mp4",
        SizeBytes = 1048576,
        ByteSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        ContentType = "video/mp4",
        CapturedAtUtc = "2026-04-20T12:00:00Z"
    };

    private readonly UploadControlPlaneSignInFormModel _signInForm = new();

    private bool _isSubmitting;
    private bool _isControlPlaneSubmitting;
    private string? _errorMessage;
    private string? _controlPlaneMessage;
    private string? _controlPlaneErrorMessage;
    private VideoPreUploadCheckResponseDto? _preUploadResponse;
    private UploadDecisionPresentationModel? _decisionModel;
    private DirectSiteVideoUploadResult? _directUploadResult;
    private UploadReceiptFormModel? _receiptForm;
    private VideoUploadReceiptResponseDto? _receiptResponse;
    private UploadReceiptPresentationModel? _receiptModel;
    private UploadControlPlaneSanitizedSession? _sessionSummary;
    private IReadOnlyList<UploadControlPlaneGroupNode> _groupNodes = Array.Empty<UploadControlPlaneGroupNode>();

    [Inject]
    public IVideoUploadApi VideoUploadApi { get; set; } = default!;

    [Inject]
    public IDirectSiteVideoUploadAdapter DirectSiteVideoUploadAdapter { get; set; } = default!;

    [Inject]
    public IUploadControlPlaneApi UploadControlPlaneApi { get; set; } = default!;

    [Inject]
    public IUploadControlPlaneSessionStore SessionStore { get; set; } = default!;

    [Inject]
    public ILogger<UploadPage> Logger { get; set; } = default!;

    private bool CanRunDirectUploadBoundary =>
        !_isSubmitting &&
        _preUploadResponse is not null &&
        _decisionModel?.CanContinue == true;

    private bool CanSubmitUploadReceipt =>
        !_isSubmitting &&
        _receiptForm is not null &&
        _directUploadResult?.Succeeded == true;

    protected override async Task OnInitializedAsync()
    {
        await LoadStoredSessionSummaryAsync();
    }

    private async Task SignInControlPlaneAsync()
    {
        _isControlPlaneSubmitting = true;
        _controlPlaneMessage = null;
        _controlPlaneErrorMessage = null;

        try
        {
            var request = UploadControlPlaneSessionFactory.CreateSignInRequest(_signInForm);
            var response = await UploadControlPlaneApi.SignInAsync(request);
            var session = UploadControlPlaneSessionFactory.CreateSession(
                response,
                DateTimeOffset.UtcNow);

            await SessionStore.SetAsync(session);
            _sessionSummary = session.ToSanitized();
            _controlPlaneMessage = "Control plane session is ready. Token values are hidden.";
        }
        catch (Exception exception)
        {
            LogControlPlaneSignInFailed(Logger, exception);
            _controlPlaneErrorMessage = SafeMessage(exception);
        }
        finally
        {
            _signInForm.Password = string.Empty;
            _isControlPlaneSubmitting = false;
        }
    }

    private async Task LoadGroupTreeNodesAsync()
    {
        _isControlPlaneSubmitting = true;
        _controlPlaneMessage = null;
        _controlPlaneErrorMessage = null;

        try
        {
            var session = await SessionStore.GetAsync();

            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                _controlPlaneErrorMessage = "Control plane session is required before group tree load.";
                return;
            }

            _groupNodes = await UploadControlPlaneApi.GetGroupTreeNodesAsync(session.AccessToken);
            _controlPlaneMessage = $"{_groupNodes.Count} group tree nodes loaded.";
        }
        catch (Exception exception)
        {
            LogControlPlaneGroupTreeLoadFailed(Logger, exception);
            _controlPlaneErrorMessage = SafeMessage(exception);
        }
        finally
        {
            _isControlPlaneSubmitting = false;
        }
    }

    private void UseGroupNode(UploadControlPlaneGroupNode node)
    {
        if (!node.Id.HasValue)
        {
            _controlPlaneErrorMessage = "Selected group node does not have an id.";
            return;
        }

        _form.GroupNodeId = node.Id.Value.ToString();
        _controlPlaneMessage = $"GroupNodeId selected: {_form.GroupNodeId}";
        _controlPlaneErrorMessage = null;
    }

    private async Task ClearControlPlaneSessionAsync()
    {
        await SessionStore.ClearAsync();

        _sessionSummary = null;
        _groupNodes = Array.Empty<UploadControlPlaneGroupNode>();
        _controlPlaneMessage = "Control plane session cleared.";
        _controlPlaneErrorMessage = null;
        _signInForm.Password = string.Empty;
    }

    private async Task RunPreUploadCheckAsync()
    {
        _isSubmitting = true;
        _errorMessage = null;
        _preUploadResponse = null;
        _decisionModel = null;
        _directUploadResult = null;
        _receiptForm = null;
        _receiptResponse = null;
        _receiptModel = null;

        try
        {
            var request = UploadPreCheckRequestFactory.Create(_form);
            var response = await VideoUploadApi.CheckPreUploadAsync(request);

            _preUploadResponse = response;
            _decisionModel = UploadDecisionPresentation.FromDecision(response.Decision);

            if (_decisionModel.IsUnsupported)
            {
                LogUnsupportedPreUploadDecision(Logger, response.Decision, null);
            }
        }
        catch (Exception exception)
        {
            LogUploadScreenPreUploadCheckFailed(Logger, exception);
            _errorMessage = SafeMessage(exception);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task RunDirectSiteUploadBoundaryAsync()
    {
        if (_preUploadResponse is null)
        {
            _errorMessage = "PreUploadCheck must complete before direct site upload boundary can run.";
            return;
        }

        _isSubmitting = true;
        _errorMessage = null;
        _directUploadResult = null;
        _receiptForm = null;
        _receiptResponse = null;
        _receiptModel = null;

        try
        {
            var draft = new DirectSiteVideoUploadDraft(
                PreUploadCheckId: _preUploadResponse.PreUploadCheckId,
                FileName: _form.FileName,
                SizeBytes: _form.SizeBytes,
                ByteSha256: _form.ByteSha256,
                ContentType: _form.ContentType);

            await using var content = new MemoryStream(Array.Empty<byte>());
            var uploadResult = await DirectSiteVideoUploadAdapter.UploadAsync(draft, content);

            _directUploadResult = uploadResult;

            if (uploadResult.Succeeded)
            {
                _receiptForm = CreateReceiptForm(uploadResult);
            }
            else
            {
                LogDirectSiteUploadAdapterDidNotComplete(Logger, uploadResult.Message, null);
            }
        }
        catch (Exception exception)
        {
            LogDirectSiteUploadAdapterBoundaryFailed(Logger, exception);
            _errorMessage = SafeMessage(exception);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task SubmitUploadReceiptAsync()
    {
        if (_receiptForm is null)
        {
            _errorMessage = "UploadReceipt payload is not ready.";
            return;
        }

        _isSubmitting = true;
        _errorMessage = null;
        _receiptResponse = null;
        _receiptModel = null;

        try
        {
            var request = UploadReceiptRequestFactory.Create(_receiptForm);
            var response = await VideoUploadApi.SubmitUploadReceiptAsync(request);

            _receiptResponse = response;
            _receiptModel = UploadReceiptPresentation.FromStatus(response.Status);

            if (_receiptModel.IsUnsupported)
            {
                LogUnsupportedUploadReceiptStatus(Logger, response.Status, null);
            }
        }
        catch (Exception exception)
        {
            LogUploadReceiptSubmissionFailed(Logger, exception);
            _errorMessage = SafeMessage(exception);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private UploadReceiptFormModel CreateReceiptForm(DirectSiteVideoUploadResult uploadResult)
    {
        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var idempotencyKey = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        return new UploadReceiptFormModel
        {
            PreUploadCheckId = _preUploadResponse?.PreUploadCheckId.ToString() ?? string.Empty,
            IdempotencyKey = idempotencyKey,
            UserId = _form.UserId,
            DeviceId = _form.DeviceId,
            GroupNodeId = _form.GroupNodeId,
            ExternalVideoId = uploadResult.ExternalVideoId ?? "site-video-not-configured",
            StorageKey = uploadResult.StorageKey ?? "videos/site-video-not-configured.mp4",
            SiteStatus = uploadResult.SiteStatus ?? "uploaded",
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
    }

    private async Task LoadStoredSessionSummaryAsync()
    {
        var session = await SessionStore.GetAsync();
        _sessionSummary = session?.ToSanitized();
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
}