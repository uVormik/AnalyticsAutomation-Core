using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using BuildingBlocks.Contracts.Auth;
using BuildingBlocks.Contracts.VideoUpload;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace App.Api.IntegrationTests;

public sealed class UploadControlPlaneAuthorizationTests(AppApiFactory factory) : IClassFixture<AppApiFactory>
{
    [Fact]
    public async Task PreUploadCheckRejectsAnonymous()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/video/pre-upload-check",
            CreatePreUploadCheckRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadReceiptRejectsAnonymous()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/video/upload-receipt",
            CreateAnonymousUploadReceiptRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PreUploadCheckAllowsAuthenticated()
    {
        using HttpClient client = factory.CreateClient();
        Guid userId = await SignInAsync(client);

        VideoPreUploadCheckRequestDto request = CreatePreUploadCheckRequest(userId);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/video/pre-upload-check",
            request);

        await AssertStatusCodeAsync("pre upload check", HttpStatusCode.OK, response);

        VideoPreUploadCheckResponseDto? body =
            await response.Content.ReadFromJsonAsync<VideoPreUploadCheckResponseDto>();

        Assert.NotNull(body);
        Assert.Equal(PreUploadCheckDecisions.Allow, body.Decision);
        Assert.True(body.CanUploadToSite);
        Assert.Null(body.ExistingPreUploadCheckId);
        Assert.Contains("UPLOAD_TO_SITE_DIRECT", body.RequiredNextSteps);
        Assert.Contains("SEND_UPLOAD_RECEIPT", body.RequiredNextSteps);
        Assert.NotNull(body.SitePlan);
        Assert.Equal("/api/video/upload-receipt", body.SitePlan.RequiredReceiptEndpoint);
    }

    [Fact]
    public async Task UploadReceiptAllowsAuthenticatedAndIsIdempotent()
    {
        using HttpClient client = factory.CreateClient();
        Guid userId = await SignInAsync(client);

        VideoPreUploadCheckRequestDto preUploadRequest = CreatePreUploadCheckRequest(userId);

        using HttpResponseMessage preUploadResponseMessage = await client.PostAsJsonAsync(
            "/api/video/pre-upload-check",
            preUploadRequest);

        await AssertStatusCodeAsync("pre upload check", HttpStatusCode.OK, preUploadResponseMessage);

        VideoPreUploadCheckResponseDto? preUploadResponse =
            await preUploadResponseMessage.Content.ReadFromJsonAsync<VideoPreUploadCheckResponseDto>();

        Assert.NotNull(preUploadResponse);
        Assert.True(preUploadResponse.CanUploadToSite);
        Assert.NotNull(preUploadResponse.SitePlan);

        string idempotencyKey = Guid.NewGuid().ToString("N");
        VideoUploadReceiptRequestDto uploadReceiptRequest =
            CreateUploadReceiptRequest(preUploadRequest, preUploadResponse, idempotencyKey);

        using HttpResponseMessage firstReceiptResponse = await client.PostAsJsonAsync(
            "/api/video/upload-receipt",
            uploadReceiptRequest);

        await AssertStatusCodeAsync("first upload receipt", HttpStatusCode.OK, firstReceiptResponse);

        VideoUploadReceiptResponseDto? firstBody =
            await firstReceiptResponse.Content.ReadFromJsonAsync<VideoUploadReceiptResponseDto>();

        Assert.NotNull(firstBody);
        Assert.Equal(VideoUploadReceiptStatuses.Accepted, firstBody.Status);
        Assert.True(firstBody.Accepted);
        Assert.False(firstBody.WasAlreadyAccepted);
        Assert.Equal(preUploadResponse.PreUploadCheckId, firstBody.PreUploadCheckId);
        Assert.Equal("queued", firstBody.AnalysisJobStatus);
        Assert.NotEqual(Guid.Empty, firstBody.UploadReceiptId);

        using HttpResponseMessage secondReceiptResponse = await client.PostAsJsonAsync(
            "/api/video/upload-receipt",
            uploadReceiptRequest);

        await AssertStatusCodeAsync("second upload receipt", HttpStatusCode.OK, secondReceiptResponse);

        VideoUploadReceiptResponseDto? secondBody =
            await secondReceiptResponse.Content.ReadFromJsonAsync<VideoUploadReceiptResponseDto>();

        Assert.NotNull(secondBody);
        Assert.Equal(VideoUploadReceiptStatuses.AlreadyAccepted, secondBody.Status);
        Assert.True(secondBody.Accepted);
        Assert.True(secondBody.WasAlreadyAccepted);
        Assert.Equal(firstBody.UploadReceiptId, secondBody.UploadReceiptId);
        Assert.Equal(firstBody.PreUploadCheckId, secondBody.PreUploadCheckId);
        Assert.Equal(firstBody.AnalysisJobStatus, secondBody.AnalysisJobStatus);
    }

    private async Task<Guid> SignInAsync(HttpClient client)
    {
        string login = $"s2-15-user-{Guid.NewGuid():N}";
        const string password = "S2-15-test-password!";

        await SeedUserAsync(login, password);

        using HttpResponseMessage signInResponse = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequestDto(
                Login: login,
                Password: password,
                DeviceId: Guid.NewGuid()));

        await AssertStatusCodeAsync("sign in", HttpStatusCode.OK, signInResponse);

        SignInResponseDto? signInBody = await signInResponse.Content.ReadFromJsonAsync<SignInResponseDto>();

        Assert.NotNull(signInBody);

        string accessToken = Assert.IsType<string>(signInBody.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return signInBody.User.UserId;
    }

    private async Task SeedUserAsync(string login, string password)
    {
        using IServiceScope scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AuthUser>>();

        string normalizedLogin = login.Trim().ToUpperInvariant();

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Login = login,
            NormalizedLogin = normalizedLogin,
            DisplayName = "S2-15 Test User",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.AuthUsers.Add(user);
        await dbContext.SaveChangesAsync();
    }

    private static VideoPreUploadCheckRequestDto CreatePreUploadCheckRequest(Guid userId)
    {
        return new VideoPreUploadCheckRequestDto(
            UserId: userId,
            DeviceId: Guid.NewGuid(),
            GroupNodeId: Guid.NewGuid(),
            BusinessObjectKey: $"business-object-{Guid.NewGuid():N}",
            FileName: $"clip-{Guid.NewGuid():N}.mp4",
            SizeBytes: 1024,
            ByteSha256: CreateHex64(),
            ContentType: "video/mp4",
            CapturedAtUtc: DateTimeOffset.UtcNow);
    }

    private static VideoUploadReceiptRequestDto CreateAnonymousUploadReceiptRequest()
    {
        VideoPreUploadCheckRequestDto preUploadRequest = CreatePreUploadCheckRequest(Guid.NewGuid());
        string externalVideoId = $"site-video-{Guid.NewGuid():N}";

        return new VideoUploadReceiptRequestDto(
            PreUploadCheckId: Guid.NewGuid(),
            UserId: preUploadRequest.UserId,
            DeviceId: preUploadRequest.DeviceId,
            GroupNodeId: preUploadRequest.GroupNodeId,
            ExternalVideoId: externalVideoId,
            StorageKey: $"videos/{externalVideoId}.mp4",
            SiteStatus: "uploaded",
            SizeBytes: preUploadRequest.SizeBytes,
            ByteSha256: preUploadRequest.ByteSha256,
            IdempotencyKey: Guid.NewGuid().ToString("N"),
            UploadedAtUtc: DateTimeOffset.UtcNow);
    }

    private static VideoUploadReceiptRequestDto CreateUploadReceiptRequest(
        VideoPreUploadCheckRequestDto preUploadRequest,
        VideoPreUploadCheckResponseDto preUploadResponse,
        string idempotencyKey)
    {
        VideoPreUploadSitePlanDto sitePlan = preUploadResponse.SitePlan
            ?? throw new InvalidOperationException("Pre-upload response did not provide a site plan.");

        return new VideoUploadReceiptRequestDto(
            PreUploadCheckId: preUploadResponse.PreUploadCheckId,
            UserId: preUploadRequest.UserId,
            DeviceId: preUploadRequest.DeviceId,
            GroupNodeId: preUploadRequest.GroupNodeId,
            ExternalVideoId: sitePlan.ExternalVideoId,
            StorageKey: sitePlan.StorageKey,
            SiteStatus: "uploaded",
            SizeBytes: preUploadRequest.SizeBytes,
            ByteSha256: preUploadRequest.ByteSha256,
            IdempotencyKey: idempotencyKey,
            UploadedAtUtc: DateTimeOffset.UtcNow);
    }

    private static string CreateHex64()
    {
        return $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    }

    private static async Task AssertStatusCodeAsync(
        string stepName,
        HttpStatusCode expectedStatus,
        HttpResponseMessage response)
    {
        if (response.StatusCode == expectedStatus)
        {
            return;
        }

        string responseBody = await response.Content.ReadAsStringAsync();
        string redactedResponseBody = RedactSensitiveTokenValues(responseBody);

        Assert.Fail(
            $"""
            Step: {stepName}
            Expected status: {expectedStatus} ({(int)expectedStatus})
            Actual status: {response.StatusCode} ({(int)response.StatusCode})
            Response body:
            {redactedResponseBody}
            """);
    }

    private static string RedactSensitiveTokenValues(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
        {
            return responseBody;
        }

        try
        {
            JsonNode? responseJson = JsonNode.Parse(responseBody);

            if (responseJson is not null)
            {
                RedactSensitiveTokenProperties(responseJson);

                return responseJson.ToJsonString(new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
        }
        catch (JsonException)
        {
        }

        return Regex.Replace(
            responseBody,
            @"(?i)(\b(?:accessToken|refreshToken)\b\s*[:=]\s*[""']?)[^""'\s,}\]]+([""']?)",
            "$1<redacted>$2");
    }

    private static void RedactSensitiveTokenProperties(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (string propertyName in jsonObject.Select(property => property.Key).ToArray())
            {
                if (IsSensitiveTokenProperty(propertyName))
                {
                    jsonObject[propertyName] = "<redacted>";
                    continue;
                }

                JsonNode? propertyValue = jsonObject[propertyName];

                if (propertyValue is not null)
                {
                    RedactSensitiveTokenProperties(propertyValue);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? item in jsonArray)
            {
                if (item is not null)
                {
                    RedactSensitiveTokenProperties(item);
                }
            }
        }
    }

    private static bool IsSensitiveTokenProperty(string propertyName)
    {
        return string.Equals(propertyName, "accessToken", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "refreshToken", StringComparison.OrdinalIgnoreCase);
    }
}