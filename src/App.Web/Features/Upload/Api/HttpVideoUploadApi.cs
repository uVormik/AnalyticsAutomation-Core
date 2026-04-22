using System.Net.Http.Json;
using System.Text.Json;

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

    public HttpVideoUploadApi(HttpClient httpClient, ILogger<HttpVideoUploadApi> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<VideoPreUploadCheckResponseDto> CheckPreUploadAsync(
        VideoPreUploadCheckRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                UploadApiEndpoints.PreUploadCheck,
                request,
                JsonOptions,
                cancellationToken);

            return await ReadRequiredJsonAsync<VideoPreUploadCheckResponseDto>(
                response,
                UploadApiEndpoints.PreUploadCheck,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
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
            using var response = await _httpClient.PostAsJsonAsync(
                UploadApiEndpoints.UploadReceipt,
                request,
                JsonOptions,
                cancellationToken);

            return await ReadRequiredJsonAsync<VideoUploadReceiptResponseDto>(
                response,
                UploadApiEndpoints.UploadReceipt,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogUploadReceiptApiCallFailed(_logger, exception);
            throw;
        }
    }

    private static async Task<TResponse> ReadRequiredJsonAsync<TResponse>(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = $"Upload API call to {endpoint} failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {body}";
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException($"Upload API call to {endpoint} returned an empty response body.");
    }
}