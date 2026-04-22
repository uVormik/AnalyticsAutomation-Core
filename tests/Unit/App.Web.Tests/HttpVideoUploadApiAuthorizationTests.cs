using System.Globalization;
using System.Net;
using System.Text.Json;

using App.Web.Features.Upload.Api;
using App.Web.Features.Upload.ControlPlane;

using BuildingBlocks.Contracts.VideoUpload;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace App.Web.Tests;

public sealed class HttpVideoUploadApiAuthorizationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CheckPreUploadAddsBearerHeaderWhenSessionExists()
    {
        var handler = new CapturingHandler(CreatePreUploadResponse());
        var sessionStore = new InMemoryUploadControlPlaneSessionStore();
        await sessionStore.SetAsync(CreateSession());

        var api = new HttpVideoUploadApi(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.example.test/")
            },
            NullLogger<HttpVideoUploadApi>.Instance,
            sessionStore);

        await api.CheckPreUploadAsync(CreatePreUploadRequest());

        Assert.Equal("Bearer", handler.LastRequest?.Headers.Authorization?.Scheme);
        Assert.Equal("alpha-sensitive-value", handler.LastRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SubmitUploadReceiptAddsBearerHeaderWhenSessionExists()
    {
        var handler = new CapturingHandler(CreateReceiptResponse());
        var sessionStore = new InMemoryUploadControlPlaneSessionStore();
        await sessionStore.SetAsync(CreateSession());

        var api = new HttpVideoUploadApi(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.example.test/")
            },
            NullLogger<HttpVideoUploadApi>.Instance,
            sessionStore);

        await api.SubmitUploadReceiptAsync(CreateReceiptRequest());

        Assert.Equal("Bearer", handler.LastRequest?.Headers.Authorization?.Scheme);
        Assert.Equal("alpha-sensitive-value", handler.LastRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task CheckPreUploadDoesNotAddBearerHeaderWhenSessionIsMissing()
    {
        var handler = new CapturingHandler(CreatePreUploadResponse());
        var api = new HttpVideoUploadApi(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.example.test/")
            },
            NullLogger<HttpVideoUploadApi>.Instance,
            new InMemoryUploadControlPlaneSessionStore());

        await api.CheckPreUploadAsync(CreatePreUploadRequest());

        Assert.Null(handler.LastRequest?.Headers.Authorization);
    }

    [Fact]
    public async Task ErrorBodyIsRedactedBeforeExceptionMessageIsSurfaced()
    {
        var handler = new FailingHandler("""
            {
                "accessToken": "alpha-sensitive-value",
                "refreshToken": "beta-sensitive-value"
            }
            """);

        var api = new HttpVideoUploadApi(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.example.test/")
            },
            NullLogger<HttpVideoUploadApi>.Instance,
            new InMemoryUploadControlPlaneSessionStore());

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            api.CheckPreUploadAsync(CreatePreUploadRequest()));

        Assert.DoesNotContain("alpha-sensitive-value", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("beta-sensitive-value", exception.Message, StringComparison.Ordinal);
        Assert.Contains("***REDACTED***", exception.Message, StringComparison.Ordinal);
    }

    private static UploadControlPlaneSession CreateSession() =>
        new(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DisplayName: "Integration Web",
            AccessToken: "alpha-sensitive-value",
            RefreshToken: "beta-sensitive-value",
            CreatedAtUtc: ParseUtc("2026-04-22T12:00:00Z"));

    private static VideoPreUploadCheckRequestDto CreatePreUploadRequest() =>
        new(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DeviceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            GroupNodeId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            BusinessObjectKey: "demo-business-object",
            FileName: "sample.mp4",
            SizeBytes: 1048576,
            ByteSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ContentType: "video/mp4",
            CapturedAtUtc: ParseUtc("2026-04-22T12:00:00Z"));

    private static VideoPreUploadCheckResponseDto CreatePreUploadResponse() =>
        new(
            PreUploadCheckId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Decision: PreUploadCheckDecisions.Allow,
            CanUploadToSite: true,
            ReasonCode: "OK",
            Message: "allowed",
            ExistingPreUploadCheckId: null,
            RequiredNextSteps: Array.Empty<string>(),
            SitePlan: null,
            CheckedAtUtc: ParseUtc("2026-04-22T12:00:00Z"));

    private static VideoUploadReceiptRequestDto CreateReceiptRequest() =>
        new(
            PreUploadCheckId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DeviceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            GroupNodeId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ExternalVideoId: "site-video-demo",
            StorageKey: "videos/site-video-demo.mp4",
            SiteStatus: "uploaded",
            SizeBytes: 1048576,
            ByteSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            IdempotencyKey: "idempotency-key-1",
            UploadedAtUtc: ParseUtc("2026-04-22T12:00:00Z"));

    private static VideoUploadReceiptResponseDto CreateReceiptResponse() =>
        new(
            UploadReceiptId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            PreUploadCheckId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Status: VideoUploadReceiptStatuses.Accepted,
            Accepted: true,
            WasAlreadyAccepted: false,
            Message: "accepted",
            AnalysisJobStatus: "QUEUED",
            ReceivedAtUtc: ParseUtc("2026-04-22T12:00:00Z"));

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly object _response;

        public CapturingHandler(object response)
        {
            _response = response;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(_response, JsonOptions))
            });
        }
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public FailingHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(_responseBody)
            });
        }
    }
}