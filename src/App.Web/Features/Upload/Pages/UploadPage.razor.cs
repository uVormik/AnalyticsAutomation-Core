using System.Globalization;
using App.Web.Features.Upload.Api;
using App.Web.Features.Upload.Models;
using App.Web.Features.Upload.Presentation;
using App.Web.Features.Upload.SiteGateway;
using BuildingBlocks.Contracts.VideoUpload;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace App.Web.Features.Upload.Pages;

public partial class UploadPage
{
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

    private bool _isSubmitting;
    private string? _errorMessage;
    private VideoPreUploadCheckResponseDto? _preUploadResponse;
    private UploadDecisionPresentationModel? _decisionModel;
    private DirectSiteVideoUploadResult? _directUploadResult;
    private UploadReceiptFormModel? _receiptForm;
    private VideoUploadReceiptResponseDto? _receiptResponse;
    private UploadReceiptPresentationModel? _receiptModel;

    [Inject]
    public IVideoUploadApi VideoUploadApi { get; set; } = default!;

    [Inject]
    public IDirectSiteVideoUploadAdapter DirectSiteVideoUploadAdapter { get; set; } = default!;

    [Inject]
    public ILoggerFactory LoggerFactory { get; set; } = default!;

    private bool CanRunDirectUploadBoundary =>
        !_isSubmitting &&
        _preUploadResponse is not null &&
        _decisionModel?.CanContinue == true;

    private bool CanSubmitUploadReceipt =>
        !_isSubmitting &&
        _receiptForm is not null &&
        _directUploadResult?.Succeeded == true;

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
                LoggerFactory
                    .CreateLogger<UploadPage>()
                    .LogWarning(
                        "Unsupported pre-upload decision returned by backend: {Decision}",
                        response.Decision);
            }
        }
        catch (Exception exception)
        {
            LoggerFactory
                .CreateLogger<UploadPage>()
                .LogError(exception, "Upload screen pre-upload check failed.");

            _errorMessage = exception.Message;
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
                LoggerFactory
                    .CreateLogger<UploadPage>()
                    .LogInformation("Direct site upload adapter did not complete: {Message}", uploadResult.Message);
            }
        }
        catch (Exception exception)
        {
            LoggerFactory
                .CreateLogger<UploadPage>()
                .LogError(exception, "Direct site upload adapter boundary failed.");

            _errorMessage = exception.Message;
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
                LoggerFactory
                    .CreateLogger<UploadPage>()
                    .LogWarning(
                        "Unsupported upload receipt status returned by backend: {Status}",
                        response.Status);
            }
        }
        catch (Exception exception)
        {
            LoggerFactory
                .CreateLogger<UploadPage>()
                .LogError(exception, "Upload receipt submission failed.");

            _errorMessage = exception.Message;
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
            ClientReceiptKey = idempotencyKey,
            IdempotencyKey = idempotencyKey,
            UserId = _form.UserId,
            DeviceId = _form.DeviceId,
            GroupNodeId = _form.GroupNodeId,
            BusinessObjectKey = _form.BusinessObjectKey,
            ExternalVideoId = uploadResult.ExternalVideoId ?? "site-video-not-configured",
            FileName = _form.FileName,
            ContentType = _form.ContentType,
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

    private static string AlertClass(string tone) => tone switch
    {
        "success" => "alert alert-success mt-3",
        "warning" => "alert alert-warning mt-3",
        "danger" => "alert alert-danger mt-3",
        "info" => "alert alert-info mt-3",
        _ => "alert alert-secondary mt-3"
    };
}