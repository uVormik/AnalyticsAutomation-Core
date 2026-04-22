using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using App.Web.Features.Upload.ControlPlane;

using BuildingBlocks.Contracts.VideoUpload;

using Microsoft.Extensions.Logging;

namespace App.Web.Features.Upload.Api;

public sealed class HttpVideoUploadApi : IVideoUploadApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, Exception?> LogPreUploadCheckApiCallFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1001, nameof(LogPreUploadCheckApiCallFailed)),
            "Pre-upload check API call failed.");

    private static readonly Action<ILogger, Exception?> LogUploadReceiptApiCallFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1002, nameof(LogUploadReceiptApiCallFailed)),
            "Upload receipt API call failed.");

    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpVideoUploadApi> _logger;
    private readonly IUploadControlPlaneSessionStore _sessionStore;

    public HttpVideoUploadApi(
        HttpClient httpClient,
        ILogger<HttpVideoUploadApi> logger,
        IUploadControlPlaneSessionStore sessionStore)
    {
        _httpClient = httpClient;
        _logger = logger;
        _sessionStore = sessionStore;
    }

    public async Task<VideoPreUploadCheckResponseDto> CheckPreUploadAsync(
        VideoPreUploadCheckRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var httpRequest = await CreateJsonRequestAsync(
                HttpMethod.Post,
                UploadApiEndpoints.PreUploadCheck,
                request,
                cancellationToken);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            return await ReadJsonOrThrowAsync<VideoPreUploadCheckResponseDto>(
                response,
                UploadApiEndpoints.PreUploadCheck,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not HttpRequestException)
        {
            LogPreUploadCheckApiCallFailed(_logger, exception);
            throw;
        }
    }

    public async Task<VideoUploadReceiptResponseDto> SubmitUploadReceiptAsync(
        VideoUploadReceiptRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var httpRequest = await CreateJsonRequestAsync(
                HttpMethod.Post,
                UploadApiEndpoints.UploadReceipt,
                request,
                cancellationToken);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            return await ReadJsonOrThrowAsync<VideoUploadReceiptResponseDto>(
                response,
                UploadApiEndpoints.UploadReceipt,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not HttpRequestException)
        {
            LogUploadReceiptApiCallFailed(_logger, exception);
            throw;
        }
    }

    private async Task<HttpRequestMessage> CreateJsonRequestAsync<T>(
        HttpMethod method,
        string endpoint,
        T payload,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, endpoint)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        var session = await _sessionStore.GetAsync(cancellationToken);

        if (session is not null && !string.IsNullOrWhiteSpace(session.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                session.AccessToken);
        }

        return request;
    }

    private static async Task<T> ReadJsonOrThrowAsync<T>(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var sanitizedBody = UploadControlPlaneErrorRedactor.Redact(rawBody);
            var statusCode = (int)response.StatusCode;

            throw new HttpRequestException(
                $"Upload API request failed for {endpoint}. Status={statusCode}. Body={sanitizedBody}",
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
            $"Upload API response for {endpoint} was empty.");
    }
}