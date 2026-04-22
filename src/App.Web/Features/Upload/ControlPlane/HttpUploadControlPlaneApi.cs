using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace App.Web.Features.Upload.ControlPlane;

public sealed class HttpUploadControlPlaneApi : IUploadControlPlaneApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, string, int, Exception?> LogControlPlaneRequestFailed =
        LoggerMessage.Define<string, int>(
            LogLevel.Error,
            new EventId(3001, nameof(LogControlPlaneRequestFailed)),
            "Upload control plane request failed for {Endpoint} with HTTP status {StatusCode}.");

    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpUploadControlPlaneApi> _logger;

    public HttpUploadControlPlaneApi(
        HttpClient httpClient,
        ILogger<HttpUploadControlPlaneApi> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UploadControlPlaneSignInResponse> SignInAsync(
        UploadControlPlaneSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync(
            UploadControlPlaneEndpoints.SignIn,
            request,
            JsonOptions,
            cancellationToken);

        return await ReadJsonOrThrowAsync<UploadControlPlaneSignInResponse>(
            response,
            UploadControlPlaneEndpoints.SignIn,
            cancellationToken);
    }

    public async Task<UploadControlPlaneSignInResponse> RefreshAsync(
        UploadControlPlaneRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync(
            UploadControlPlaneEndpoints.Refresh,
            request,
            JsonOptions,
            cancellationToken);

        return await ReadJsonOrThrowAsync<UploadControlPlaneSignInResponse>(
            response,
            UploadControlPlaneEndpoints.Refresh,
            cancellationToken);
    }

    public async Task<IReadOnlyList<UploadControlPlaneGroupNode>> GetGroupTreeNodesAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            UploadControlPlaneEndpoints.GroupTreeNodes);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            RequiredToken(accessToken));

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        return await ReadJsonOrThrowAsync<IReadOnlyList<UploadControlPlaneGroupNode>>(
            response,
            UploadControlPlaneEndpoints.GroupTreeNodes,
            cancellationToken);
    }

    private async Task<T> ReadJsonOrThrowAsync<T>(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var sanitizedBody = UploadControlPlaneErrorRedactor.Redact(rawBody);
            var statusCode = (int)response.StatusCode;

            LogControlPlaneRequestFailed(_logger, endpoint, statusCode, null);

            throw new HttpRequestException(
                $"Upload control plane request failed for {endpoint}. Status={statusCode}. Body={sanitizedBody}",
                inner: null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(
            JsonOptions,
            cancellationToken);

        if (payload is not null)
        {
            return payload;
        }

        throw new InvalidOperationException(
            $"Upload control plane response for {endpoint} was empty.");
    }

    private static string RequiredToken(string accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken;
        }

        throw new InvalidOperationException("Access token is required for upload control plane request.");
    }
}