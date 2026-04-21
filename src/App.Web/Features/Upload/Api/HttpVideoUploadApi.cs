using System.Net.Http.Json;
using System.Text.Json;

using BuildingBlocks.Contracts.VideoUpload;

using Microsoft.Extensions.Logging;

namespace App.Web.Features.Upload.Api;

public sealed class HttpVideoUploadApi : IVideoUploadApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
            _logger.LogError(exception, "Pre-upload check API call failed.");
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
            _logger.LogError(exception, "Upload receipt API call failed.");
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