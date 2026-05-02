using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class HttpDesktopPreUploadCheckClient : IDesktopPreUploadCheckClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IDesktopControlPlaneAccessTokenProvider _accessTokenProvider;
    private readonly IDesktopSessionState _sessionState;
    private readonly Uri _preUploadCheckEndpoint;

    public HttpDesktopPreUploadCheckClient(
        HttpClient httpClient,
        IDesktopControlPlaneAccessTokenProvider accessTokenProvider,
        IDesktopSessionState sessionState)
        : this(
            httpClient,
            accessTokenProvider,
            sessionState,
            new Uri("/api/video/pre-upload-check", UriKind.Relative))
    {
    }

    public HttpDesktopPreUploadCheckClient(
        HttpClient httpClient,
        IDesktopControlPlaneAccessTokenProvider accessTokenProvider,
        IDesktopSessionState sessionState,
        Uri preUploadCheckEndpoint)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(accessTokenProvider);
        ArgumentNullException.ThrowIfNull(sessionState);
        ArgumentNullException.ThrowIfNull(preUploadCheckEndpoint);

        _httpClient = httpClient;
        _accessTokenProvider = accessTokenProvider;
        _sessionState = sessionState;
        _preUploadCheckEndpoint = preUploadCheckEndpoint;
    }

    public async ValueTask<DesktopPreUploadCheckResult> CheckAsync(
        DesktopPreUploadCheckRequestPreview requestPreview,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestPreview);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            DesktopControlPlaneAccessTokenSnapshot accessToken =
                await _accessTokenProvider.GetCurrentAccessTokenAsync(cancellationToken);
            DesktopSessionSnapshot session = _sessionState.Current;

            if (!accessToken.HasAccessToken || !session.IsSignedIn || session.UserId is null)
            {
                return DesktopPreUploadCheckResult.LiveUnauthorized;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _preUploadCheckEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);
            request.Content = JsonContent.Create(
                new PreUploadCheckRequestPayload(
                    UserId: session.UserId.Value,
                    DeviceId: session.DeviceId,
                    GroupNodeId: requestPreview.GroupNodeId,
                    BusinessObjectKey: requestPreview.BusinessObjectKeyPreview,
                    FileName: requestPreview.FileName,
                    SizeBytes: requestPreview.SizeBytes,
                    ByteSha256: requestPreview.Sha256Hex,
                    ContentType: CreateSafeNullableContentType(requestPreview.ContentType),
                    CapturedAtUtc: DateTimeOffset.UtcNow),
                options: JsonOptions);

            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return DesktopPreUploadCheckResult.LiveUnauthorized;
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return DesktopPreUploadCheckResult.LiveUnavailable;
            }

            if (!response.IsSuccessStatusCode)
            {
                return DesktopPreUploadCheckResult.LiveFailed;
            }

            PreUploadCheckResponsePayload? payload =
                await response.Content.ReadFromJsonAsync<PreUploadCheckResponsePayload>(
                    JsonOptions,
                    cancellationToken);

            return TryMapResponse(payload);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DesktopPreUploadCheckResult.LiveUnavailable;
        }
        catch (HttpRequestException)
        {
            return DesktopPreUploadCheckResult.LiveUnavailable;
        }
        catch (InvalidOperationException)
        {
            return DesktopPreUploadCheckResult.LiveUnavailable;
        }
        catch (JsonException)
        {
            return DesktopPreUploadCheckResult.LiveMalformed;
        }
        catch (NotSupportedException)
        {
            return DesktopPreUploadCheckResult.LiveMalformed;
        }
    }

    private static DesktopPreUploadCheckResult TryMapResponse(PreUploadCheckResponsePayload? payload)
    {
        if (payload is null
            || payload.PreUploadCheckId is null
            || payload.PreUploadCheckId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.Decision)
            || payload.CanUploadToSite is null
            || string.IsNullOrWhiteSpace(payload.ReasonCode)
            || payload.RequiredNextSteps is null
            || payload.CheckedAtUtc is null)
        {
            return DesktopPreUploadCheckResult.LiveMalformed;
        }

        DesktopPreUploadCheckDecision? decision = payload.Decision.Trim() switch
        {
            "ALLOW" when payload.CanUploadToSite.Value => DesktopPreUploadCheckDecision.Allow,
            "ALLOW_WITH_REVIEW" when payload.CanUploadToSite.Value => DesktopPreUploadCheckDecision.AllowWithReview,
            "BLOCK_HARD_DUPLICATE" when !payload.CanUploadToSite.Value =>
                DesktopPreUploadCheckDecision.BlockHardDuplicate,
            "BLOCK_POSSIBLE_FALSIFICATION" when !payload.CanUploadToSite.Value =>
                DesktopPreUploadCheckDecision.BlockPossibleFalsification,
            _ => null
        };

        if (decision is null)
        {
            return DesktopPreUploadCheckResult.LiveMalformed;
        }

        string? sitePlanExternalVideoId = null;
        string? sitePlanStorageKey = null;
        if (decision is DesktopPreUploadCheckDecision.Allow or DesktopPreUploadCheckDecision.AllowWithReview)
        {
            if (payload.SitePlan is null
                || !string.Equals(
                    payload.SitePlan.RequiredReceiptEndpoint,
                    "/api/video/upload-receipt",
                    StringComparison.Ordinal)
                || !TryCreateSafeControlPlaneValue(
                    payload.SitePlan.ExternalVideoId,
                    allowSlash: false,
                    out sitePlanExternalVideoId)
                || !TryCreateSafeControlPlaneValue(
                    payload.SitePlan.StorageKey,
                    allowSlash: true,
                    out sitePlanStorageKey))
            {
                return DesktopPreUploadCheckResult.LiveMalformed;
            }
        }

        return DesktopPreUploadCheckResult.FromLiveDecision(
            decision.Value,
            payload.PreUploadCheckId.Value,
            sitePlanExternalVideoId,
            sitePlanStorageKey);
    }

    private static string? CreateSafeNullableContentType(string contentType)
    {
        return string.Equals(
            contentType,
            DesktopUploadSectionText.UnknownContentTypeValue,
            StringComparison.Ordinal)
            ? null
            : contentType;
    }

    private static bool TryCreateSafeControlPlaneValue(string? value, bool allowSlash, out string? safeValue)
    {
        safeValue = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > 200)
        {
            return false;
        }

        string lower = trimmed.ToLowerInvariant();
        string[] blockedFragments =
        [
            "authorization",
            "bearer",
            "password",
            "token",
            "sessionid",
            "session_id",
            "access_token",
            "refreshtoken",
            "refresh_token"
        ];

        foreach (string blockedFragment in blockedFragments)
        {
            if (lower.Contains(blockedFragment, StringComparison.Ordinal))
            {
                return false;
            }
        }

        foreach (char character in trimmed)
        {
            if (char.IsControl(character)
                || character == '\\'
                || character == ':'
                || (!allowSlash && character == '/'))
            {
                return false;
            }
        }

        safeValue = trimmed;
        return true;
    }

    private sealed record PreUploadCheckRequestPayload(
        Guid UserId,
        Guid? DeviceId,
        Guid? GroupNodeId,
        string BusinessObjectKey,
        string FileName,
        long SizeBytes,
        string ByteSha256,
        string? ContentType,
        DateTimeOffset CapturedAtUtc);

    private sealed class PreUploadCheckResponsePayload
    {
        public Guid? PreUploadCheckId { get; init; }

        public string? Decision { get; init; }

        public bool? CanUploadToSite { get; init; }

        public string? ReasonCode { get; init; }

        public string? Message { get; init; }

        public string? ExistingPreUploadCheckId { get; init; }

        public IReadOnlyCollection<string>? RequiredNextSteps { get; init; }

        public PreUploadSitePlanPayload? SitePlan { get; init; }

        public DateTimeOffset? CheckedAtUtc { get; init; }
    }

    private sealed class PreUploadSitePlanPayload
    {
        public string? Provider { get; init; }

        public string? ExternalVideoId { get; init; }

        public string? StorageKey { get; init; }

        public string? RequiredReceiptEndpoint { get; init; }
    }
}