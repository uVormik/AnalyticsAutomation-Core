using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using BuildingBlocks.Contracts.Auth;
using BuildingBlocks.Contracts.VideoUpload;
using BuildingBlocks.Contracts.WorkerPipeline;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Modules.WorkerPipeline;

namespace App.Api.IntegrationTests;

public sealed class UploadControlPlaneAuthorizationTests(AppApiFactory factory) : IClassFixture<AppApiFactory>
{
    private static readonly Guid BranchGroupNodeId = Guid.Parse("D4D74008-0ED5-4E46-B0A1-91E0628079C0");
    private static readonly Guid RootGroupNodeId = Guid.Parse("C15EE7FE-7C9A-4B31-8B7E-C278B59F318C");
    private static readonly Guid BranchAdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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
    public async Task UploadReceiptSyncRejectsAnonymous()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/video/upload-receipt-sync",
            CreateAnonymousUploadReceiptSyncRequest());

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

    [Fact]
    public async Task UploadReceiptSyncAllowsAuthenticatedLateSyncReceipt()
    {
        using HttpClient client = factory.CreateClient();
        var auth = await SignInWithContextAsync(client);

        VideoUploadReceiptSyncRequestDto request = CreateUploadReceiptSyncRequest(
            auth,
            CreateHex64(),
            Guid.NewGuid().ToString("N"));

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/video/upload-receipt-sync",
            request);

        await AssertStatusCodeAsync("upload receipt sync", HttpStatusCode.OK, response);

        VideoUploadReceiptResponseDto? body =
            await response.Content.ReadFromJsonAsync<VideoUploadReceiptResponseDto>();

        Assert.NotNull(body);
        Assert.Equal(VideoUploadReceiptStatuses.Accepted, body.Status);
        Assert.True(body.Accepted);
        Assert.False(body.WasAlreadyAccepted);

        using IServiceScope scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var precheck = await db.VideoUploadPreUploadChecks.SingleAsync(item => item.Id == body.PreUploadCheckId);

        Assert.Equal("late_sync_reconciled", precheck.ReasonCode);
        Assert.Equal(1, await db.VideoUploadReceipts.CountAsync(item => item.Id == body.UploadReceiptId));
        Assert.Equal(1, await db.VideoUploadReceiptAnalysisJobs.CountAsync(item => item.UploadReceiptId == body.UploadReceiptId));
    }

    [Fact]
    public async Task UploadReceiptSyncIsIdempotentByIdempotencyKey()
    {
        using HttpClient client = factory.CreateClient();
        var auth = await SignInWithContextAsync(client);

        string idempotencyKey = Guid.NewGuid().ToString("N");
        VideoUploadReceiptSyncRequestDto request = CreateUploadReceiptSyncRequest(
            auth,
            CreateHex64(),
            idempotencyKey);

        using HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "/api/video/upload-receipt-sync",
            request);

        await AssertStatusCodeAsync("first upload receipt sync", HttpStatusCode.OK, firstResponse);

        using HttpResponseMessage secondResponse = await client.PostAsJsonAsync(
            "/api/video/upload-receipt-sync",
            request);

        await AssertStatusCodeAsync("second upload receipt sync", HttpStatusCode.OK, secondResponse);

        VideoUploadReceiptResponseDto? firstBody =
            await firstResponse.Content.ReadFromJsonAsync<VideoUploadReceiptResponseDto>();
        VideoUploadReceiptResponseDto? secondBody =
            await secondResponse.Content.ReadFromJsonAsync<VideoUploadReceiptResponseDto>();

        Assert.NotNull(firstBody);
        Assert.NotNull(secondBody);
        Assert.Equal(VideoUploadReceiptStatuses.Accepted, firstBody.Status);
        Assert.Equal(VideoUploadReceiptStatuses.AlreadyAccepted, secondBody.Status);
        Assert.Equal(firstBody.UploadReceiptId, secondBody.UploadReceiptId);

        using IServiceScope scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        Assert.Equal(1, await db.VideoUploadReceipts.CountAsync(item => item.IdempotencyKey == idempotencyKey));
        Assert.Equal(1, await db.VideoUploadReceiptAnalysisJobs.CountAsync(item => item.UploadReceiptId == firstBody.UploadReceiptId));
    }

    [Fact]
    public async Task UploadReceiptSyncCanTriggerDuplicateCandidateAndIncidentPath()
    {
        await EnsureBranchAdminRoutingAsync();

        using HttpClient client = factory.CreateClient();
        var auth = await SignInWithContextAsync(client);
        string duplicateHash = CreateHex64();

        VideoUploadReceiptSyncRequestDto firstRequest = CreateUploadReceiptSyncRequest(
            auth,
            duplicateHash,
            idempotencyKey: "sync-duplicate-1",
            groupNodeId: BranchGroupNodeId,
            businessObjectKey: $"business-object-{Guid.NewGuid():N}");

        VideoUploadReceiptSyncRequestDto secondRequest = CreateUploadReceiptSyncRequest(
            auth,
            duplicateHash,
            idempotencyKey: "sync-duplicate-2",
            groupNodeId: BranchGroupNodeId,
            businessObjectKey: $"business-object-{Guid.NewGuid():N}");

        using HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "/api/video/upload-receipt-sync",
            firstRequest);
        using HttpResponseMessage secondResponse = await client.PostAsJsonAsync(
            "/api/video/upload-receipt-sync",
            secondRequest);

        await AssertStatusCodeAsync("first duplicate upload receipt sync", HttpStatusCode.OK, firstResponse);
        await AssertStatusCodeAsync("second duplicate upload receipt sync", HttpStatusCode.OK, secondResponse);

        VideoUploadReceiptResponseDto? firstBody =
            await firstResponse.Content.ReadFromJsonAsync<VideoUploadReceiptResponseDto>();
        VideoUploadReceiptResponseDto? secondBody =
            await secondResponse.Content.ReadFromJsonAsync<VideoUploadReceiptResponseDto>();

        Assert.NotNull(firstBody);
        Assert.NotNull(secondBody);

        using IServiceScope scope = factory.Services.CreateScope();
        var bridge = scope.ServiceProvider.GetRequiredService<IUploadReceiptPipelineBridgeService>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var targetReceiptIds = new[] { firstBody.UploadReceiptId, secondBody.UploadReceiptId };

        for (var attempt = 0; attempt < 16; attempt++)
        {
            _ = await bridge.ProcessNextQueuedAsync(CancellationToken.None);

            var completedTargetCount = await db.VideoUploadReceipts
                .CountAsync(
                    item => targetReceiptIds.Contains(item.Id) &&
                        item.AnalysisJobStatus == WorkerPipelineJobStatusesV1.Completed);

            if (completedTargetCount == targetReceiptIds.Length)
            {
                break;
            }
        }

        var targetAssetIds = await db.VideoDuplicateAssets
            .Where(item => targetReceiptIds.Contains(item.UploadReceiptId))
            .Select(item => item.Id)
            .ToArrayAsync();

        var duplicateCandidate = await db.VideoDuplicateCandidates.SingleAsync(
            item => targetAssetIds.Contains(item.SourceVideoAssetId) || targetAssetIds.Contains(item.MatchedVideoAssetId));
        var incident = await db.DuplicateIncidentRecords.SingleAsync(item => item.DuplicateCandidateId == duplicateCandidate.Id);
        var assignment = await db.DuplicateIncidentAssignmentRecords.SingleAsync(item => item.IncidentId == incident.Id);

        Assert.Equal(2, targetAssetIds.Length);
        Assert.Equal(duplicateCandidate.Id, incident.DuplicateCandidateId);
        Assert.Equal(BranchAdminUserId, assignment.AssignedAdminUserId);
        Assert.Equal(2, await db.VideoUploadReceipts.CountAsync(item => item.ByteSha256 == duplicateHash));
    }

    [Fact]
    public async Task UploadReceiptOnlinePathStrictValidationRemainsUnchanged()
    {
        using HttpClient client = factory.CreateClient();
        var auth = await SignInWithContextAsync(client);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/video/upload-receipt",
            CreateInvalidOnlineUploadReceiptRequest(auth));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("PreUploadCheck was not found.", responseBody, StringComparison.Ordinal);
    }

    private async Task<Guid> SignInAsync(HttpClient client)
    {
        return (await SignInWithContextAsync(client)).UserId;
    }

    private async Task<AuthenticatedClientContext> SignInWithContextAsync(HttpClient client)
    {
        string login = $"s2-15-user-{Guid.NewGuid():N}";
        const string password = "S2-15-test-password!";
        Guid deviceId = Guid.NewGuid();

        await SeedUserAsync(login, password);

        using HttpResponseMessage signInResponse = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new SignInRequestDto(
                Login: login,
                Password: password,
                DeviceId: deviceId));

        await AssertStatusCodeAsync("sign in", HttpStatusCode.OK, signInResponse);

        SignInResponseDto? signInBody = await signInResponse.Content.ReadFromJsonAsync<SignInResponseDto>();

        Assert.NotNull(signInBody);

        string accessToken = Assert.IsType<string>(signInBody.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return new AuthenticatedClientContext(signInBody.User.UserId, deviceId);
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

    private static VideoUploadReceiptSyncRequestDto CreateAnonymousUploadReceiptSyncRequest()
    {
        return new VideoUploadReceiptSyncRequestDto(
            UserId: Guid.NewGuid(),
            DeviceId: Guid.NewGuid(),
            GroupNodeId: Guid.NewGuid(),
            BusinessObjectKey: $"business-object-{Guid.NewGuid():N}",
            FileName: $"offline-{Guid.NewGuid():N}.mp4",
            ContentType: "video/mp4",
            ExternalVideoId: $"site-video-{Guid.NewGuid():N}",
            StorageKey: $"videos/site-video-{Guid.NewGuid():N}.mp4",
            SiteStatus: "uploaded",
            SizeBytes: 1024,
            ByteSha256: CreateHex64(),
            IdempotencyKey: Guid.NewGuid().ToString("N"),
            CapturedAtUtc: DateTimeOffset.UtcNow,
            UploadedAtUtc: DateTimeOffset.UtcNow);
    }

    private static VideoUploadReceiptSyncRequestDto CreateUploadReceiptSyncRequest(
        AuthenticatedClientContext auth,
        string hash,
        string idempotencyKey,
        Guid? groupNodeId = null,
        string? businessObjectKey = null)
    {
        string externalVideoId = $"site-video-sync-{Guid.NewGuid():N}";

        return new VideoUploadReceiptSyncRequestDto(
            UserId: auth.UserId,
            DeviceId: auth.DeviceId,
            GroupNodeId: groupNodeId ?? Guid.NewGuid(),
            BusinessObjectKey: businessObjectKey ?? $"business-object-{Guid.NewGuid():N}",
            FileName: $"offline-{Guid.NewGuid():N}.mp4",
            ContentType: "video/mp4",
            ExternalVideoId: externalVideoId,
            StorageKey: $"videos/{externalVideoId}.mp4",
            SiteStatus: "uploaded",
            SizeBytes: 1024,
            ByteSha256: hash,
            IdempotencyKey: idempotencyKey,
            CapturedAtUtc: DateTimeOffset.UtcNow,
            UploadedAtUtc: DateTimeOffset.UtcNow);
    }

    private static VideoUploadReceiptRequestDto CreateInvalidOnlineUploadReceiptRequest(
        AuthenticatedClientContext auth)
    {
        string externalVideoId = $"site-video-{Guid.NewGuid():N}";

        return new VideoUploadReceiptRequestDto(
            PreUploadCheckId: Guid.NewGuid(),
            UserId: auth.UserId,
            DeviceId: auth.DeviceId,
            GroupNodeId: Guid.NewGuid(),
            ExternalVideoId: externalVideoId,
            StorageKey: $"videos/{externalVideoId}.mp4",
            SiteStatus: "uploaded",
            SizeBytes: 1024,
            ByteSha256: CreateHex64(),
            IdempotencyKey: Guid.NewGuid().ToString("N"),
            UploadedAtUtc: DateTimeOffset.UtcNow);
    }

    private static string CreateHex64()
    {
        return $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    }

    private async Task EnsureBranchAdminRoutingAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        if (!await db.GroupNodes.AnyAsync(item => item.Id == RootGroupNodeId))
        {
            db.GroupNodes.Add(new GroupNode
            {
                Id = RootGroupNodeId,
                ParentNodeId = null,
                Code = "root",
                Name = "Root",
                Depth = 0,
                IsActive = true
            });
        }

        if (!await db.GroupNodes.AnyAsync(item => item.Id == BranchGroupNodeId))
        {
            db.GroupNodes.Add(new GroupNode
            {
                Id = BranchGroupNodeId,
                ParentNodeId = RootGroupNodeId,
                Code = "branch-a",
                Name = "Branch A",
                Depth = 1,
                IsActive = true
            });
        }

        if (!await db.GroupAdminAssignments.AnyAsync(
                item => item.GroupNodeId == BranchGroupNodeId && item.UserId == BranchAdminUserId))
        {
            db.GroupAdminAssignments.Add(new GroupAdminAssignment
            {
                GroupNodeId = BranchGroupNodeId,
                UserId = BranchAdminUserId,
                AssignedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync();
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

    private sealed record AuthenticatedClientContext(Guid UserId, Guid DeviceId);
}