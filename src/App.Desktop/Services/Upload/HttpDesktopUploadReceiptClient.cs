using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class HttpDesktopUploadReceiptClient : IDesktopUploadReceiptClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IDesktopControlPlaneAccessTokenProvider _accessTokenProvider;
    private readonly IDesktopSessionState _sessionState;
    private readonly Uri _uploadReceiptEndpoint;

    public HttpDesktopUploadReceiptClient(
        HttpClient httpClient,
        IDesktopControlPlaneAccessTokenProvider accessTokenProvider,
        IDesktopSessionState sessionState)
        : this(
            httpClient,
            accessTokenProvider,
            sessionState,
            new Uri("/api/video/upload-receipt", UriKind.Relative))
    {
    }

    public HttpDesktopUploadReceiptClient(
        HttpClient httpClient,
        IDesktopControlPlaneAccessTokenProvider accessTokenProvider,
        IDesktopSessionState sessionState,
        Uri uploadReceiptEndpoint)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(accessTokenProvider);
        ArgumentNullException.ThrowIfNull(sessionState);
        ArgumentNullException.ThrowIfNull(uploadReceiptEndpoint);

        _httpClient = httpClient;
        _accessTokenProvider = accessTokenProvider;
        _sessionState = sessionState;
        _uploadReceiptEndpoint = uploadReceiptEndpoint;
    }

    public async ValueTask<DesktopUploadReceiptResult> CreateAsync(
        DesktopUploadReceiptRequestPreview requestPreview,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestPreview);
        cancellationToken.ThrowIfCancellationRequested();

        if (requestPreview.PreUploadCheckId is null || requestPreview.PreUploadCheckId == Guid.Empty)
        {
            return DesktopUploadReceiptResult.LiveMalformed;
        }

        try
        {
            DesktopControlPlaneAccessTokenSnapshot accessToken =
                await _accessTokenProvider.GetCurrentAccessTokenAsync(cancellationToken);
            DesktopSessionSnapshot session = _sessionState.Current;

            if (!accessToken.HasAccessToken || !session.IsSignedIn || session.UserId is null)
            {
                return DesktopUploadReceiptResult.LiveUnauthorized;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _uploadReceiptEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);
            request.Content = JsonContent.Create(
                new UploadReceiptRequestPayload(
                    PreUploadCheckId: requestPreview.PreUploadCheckId.Value,
                    UserId: session.UserId.Value,
                    DeviceId: session.DeviceId,
                    GroupNodeId: requestPreview.GroupNodeId,
                    ExternalVideoId: requestPreview.ExternalVideoId,
                    StorageKey: requestPreview.SiteStorageKey,
                    SiteStatus: requestPreview.SiteUploadStatusPreview,
                    SizeBytes: requestPreview.SizeBytes,
                    ByteSha256: requestPreview.Sha256Hex,
                    IdempotencyKey: CreateIdempotencyKey(requestPreview.PreUploadCheckId.Value),
                    UploadedAtUtc: DateTimeOffset.UtcNow),
                options: JsonOptions);

            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return DesktopUploadReceiptResult.LiveUnauthorized;
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return DesktopUploadReceiptResult.LiveUnavailable;
            }

            if (!response.IsSuccessStatusCode)
            {
                return DesktopUploadReceiptResult.LiveFailed;
            }

            UploadReceiptResponsePayload? payload =
                await response.Content.ReadFromJsonAsync<UploadReceiptResponsePayload>(
                    JsonOptions,
                    cancellationToken);

            return TryMapResponse(payload, requestPreview);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DesktopUploadReceiptResult.LiveUnavailable;
        }
        catch (HttpRequestException)
        {
            return DesktopUploadReceiptResult.LiveUnavailable;
        }
        catch (InvalidOperationException)
        {
            return DesktopUploadReceiptResult.LiveUnavailable;
        }
        catch (JsonException)
        {
            return DesktopUploadReceiptResult.LiveMalformed;
        }
        catch (NotSupportedException)
        {
            return DesktopUploadReceiptResult.LiveMalformed;
        }
    }

    private static DesktopUploadReceiptResult TryMapResponse(
        UploadReceiptResponsePayload? payload,
        DesktopUploadReceiptRequestPreview requestPreview)
    {
        if (payload is null
            || payload.UploadReceiptId is null
            || payload.UploadReceiptId == Guid.Empty
            || payload.PreUploadCheckId is null
            || payload.PreUploadCheckId == Guid.Empty
            || payload.PreUploadCheckId != requestPreview.PreUploadCheckId
            || string.IsNullOrWhiteSpace(payload.Status)
            || payload.Accepted is null
            || payload.WasAlreadyAccepted is null
            || string.IsNullOrWhiteSpace(payload.AnalysisJobStatus)
            || payload.ReceivedAtUtc is null)
        {
            return DesktopUploadReceiptResult.LiveMalformed;
        }

        return payload.Status.Trim() switch
        {
            "ACCEPTED" when payload.Accepted.Value && !payload.WasAlreadyAccepted.Value =>
                DesktopUploadReceiptResult.FromLiveAccepted(payload.UploadReceiptId.Value, wasAlreadyAccepted: false),
            "ALREADY_ACCEPTED" when payload.Accepted.Value && payload.WasAlreadyAccepted.Value =>
                DesktopUploadReceiptResult.FromLiveAccepted(payload.UploadReceiptId.Value, wasAlreadyAccepted: true),
            _ => DesktopUploadReceiptResult.LiveMalformed
        };
    }

    private static string CreateIdempotencyKey(Guid preUploadCheckId)
    {
        return $"desktop-upload-receipt-{preUploadCheckId:N}";
    }

    private sealed record UploadReceiptRequestPayload(
        Guid PreUploadCheckId,
        Guid UserId,
        Guid? DeviceId,
        Guid? GroupNodeId,
        string ExternalVideoId,
        string StorageKey,
        string SiteStatus,
        long SizeBytes,
        string ByteSha256,
        string IdempotencyKey,
        DateTimeOffset UploadedAtUtc);

    private sealed class UploadReceiptResponsePayload
    {
        public Guid? UploadReceiptId { get; init; }

        public Guid? PreUploadCheckId { get; init; }

        public string? Status { get; init; }

        public bool? Accepted { get; init; }

        public bool? WasAlreadyAccepted { get; init; }

        public string? Message { get; init; }

        public string? AnalysisJobStatus { get; init; }

        public DateTimeOffset? ReceivedAtUtc { get; init; }
    }
}