using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using BuildingBlocks.Contracts.Auth;
using BuildingBlocks.Contracts.Incidents;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Modules.Incidents;

namespace App.Api.IntegrationTests;

public sealed class DuplicateIncidentAuthorizationTests(AppApiFactory factory) : IClassFixture<AppApiFactory>
{
    [Fact]
    public async Task AssignedMeRejectsAnonymous()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/incidents/duplicates/assigned/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DecisionRejectsAnonymous()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/incidents/duplicates/{Guid.NewGuid():D}/decision",
            new DuplicateIncidentDecisionRequestV1Dto(
                DecidedByUserId: Guid.NewGuid(),
                Decision: DuplicateIncidentV1DecisionTypes.ConfirmDuplicate,
                Notes: "Anonymous request should be rejected."));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AssignedAdminCanListOwnAssignedDuplicateIncidents()
    {
        using HttpClient assignedAdminClient = factory.CreateClient();
        using HttpClient otherAdminClient = factory.CreateClient();

        var assignedAdmin = await SignInWithContextAsync(assignedAdminClient);
        var otherAdmin = await SignInWithContextAsync(otherAdminClient);

        var ownIncident = await CreateAssignedIncidentAsync(assignedAdmin.UserId);
        _ = await CreateAssignedIncidentAsync(otherAdmin.UserId);

        using HttpResponseMessage response = await assignedAdminClient.GetAsync("/api/incidents/duplicates/assigned/me");

        await AssertStatusCodeAsync("assigned incidents me", HttpStatusCode.OK, response);

        AssignedDuplicateIncidentsResponseV1Dto? body =
            await response.Content.ReadFromJsonAsync<AssignedDuplicateIncidentsResponseV1Dto>();

        Assert.NotNull(body);
        Assert.Equal(assignedAdmin.UserId, body.AssignedAdminUserId);
        Assert.Single(body.Incidents);
        Assert.Equal(ownIncident.IncidentId, body.Incidents.Single().IncidentId);
        Assert.Equal(assignedAdmin.UserId, body.Incidents.Single().Assignments.Single().AssignedAdminUserId);
    }

    [Fact]
    public async Task CallerCannotReadAnotherAdminAssignedIncidentsByRouteIdSubstitution()
    {
        using HttpClient assignedAdminClient = factory.CreateClient();
        using HttpClient callerClient = factory.CreateClient();

        var assignedAdmin = await SignInWithContextAsync(assignedAdminClient);
        _ = await SignInWithContextAsync(callerClient);

        _ = await CreateAssignedIncidentAsync(assignedAdmin.UserId);

        using HttpResponseMessage response = await callerClient.GetAsync(
            $"/api/incidents/duplicates/assigned/{assignedAdmin.UserId:D}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignedAdminCanRecordDecision()
    {
        using HttpClient assignedAdminClient = factory.CreateClient();

        var assignedAdmin = await SignInWithContextAsync(assignedAdminClient);
        var incident = await CreateAssignedIncidentAsync(assignedAdmin.UserId);

        using HttpResponseMessage response = await assignedAdminClient.PostAsJsonAsync(
            $"/api/incidents/duplicates/{incident.IncidentId:D}/decision",
            new DuplicateIncidentDecisionRequestV1Dto(
                DecidedByUserId: assignedAdmin.UserId,
                Decision: DuplicateIncidentV1DecisionTypes.ConfirmDuplicate,
                Notes: "Confirmed by assigned admin."));

        await AssertStatusCodeAsync("record duplicate incident decision", HttpStatusCode.OK, response);

        DuplicateIncidentV1Dto? body = await response.Content.ReadFromJsonAsync<DuplicateIncidentV1Dto>();

        Assert.NotNull(body);
        Assert.Equal(DuplicateIncidentV1Statuses.Resolved, body.Status);
        Assert.NotNull(body.LatestDecision);
        Assert.Equal(assignedAdmin.UserId, body.LatestDecision!.DecidedByUserId);

        using IServiceScope scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var decision = await db.DuplicateIncidentDecisionRecords.SingleAsync(item => item.IncidentId == incident.IncidentId);

        Assert.Equal(assignedAdmin.UserId, decision.DecidedByUserId);
    }

    [Fact]
    public async Task NonAssignedUserCannotRecordDecision()
    {
        using HttpClient assignedAdminClient = factory.CreateClient();
        using HttpClient callerClient = factory.CreateClient();

        var assignedAdmin = await SignInWithContextAsync(assignedAdminClient);
        var caller = await SignInWithContextAsync(callerClient);
        var incident = await CreateAssignedIncidentAsync(assignedAdmin.UserId);

        using HttpResponseMessage response = await callerClient.PostAsJsonAsync(
            $"/api/incidents/duplicates/{incident.IncidentId:D}/decision",
            new DuplicateIncidentDecisionRequestV1Dto(
                DecidedByUserId: caller.UserId,
                Decision: DuplicateIncidentV1DecisionTypes.ConfirmDuplicate,
                Notes: "Caller is not assigned."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await CountDecisionRecordsAsync(incident.IncidentId));
    }

    [Fact]
    public async Task MismatchedDecidedByUserIdCannotSpoofAnotherAdmin()
    {
        using HttpClient assignedAdminClient = factory.CreateClient();
        using HttpClient spoofedAdminClient = factory.CreateClient();

        var assignedAdmin = await SignInWithContextAsync(assignedAdminClient);
        var spoofedAdmin = await SignInWithContextAsync(spoofedAdminClient);
        var incident = await CreateAssignedIncidentAsync(assignedAdmin.UserId);

        using HttpResponseMessage response = await assignedAdminClient.PostAsJsonAsync(
            $"/api/incidents/duplicates/{incident.IncidentId:D}/decision",
            new DuplicateIncidentDecisionRequestV1Dto(
                DecidedByUserId: spoofedAdmin.UserId,
                Decision: DuplicateIncidentV1DecisionTypes.ConfirmDuplicate,
                Notes: "Spoof another admin id."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await CountDecisionRecordsAsync(incident.IncidentId));
    }

    private async Task<DuplicateIncidentV1Dto> CreateAssignedIncidentAsync(Guid assignedAdminUserId)
    {
        using IServiceScope scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IDuplicateIncidentRoutingService>();

        return await service.CreateFromCandidateAsync(
            new DuplicateIncidentCreateRequestV1Dto(
                DuplicateCandidateId: Guid.NewGuid(),
                SourceVideoAssetId: Guid.NewGuid(),
                MatchedVideoAssetId: Guid.NewGuid(),
                UploaderUserId: Guid.NewGuid(),
                UploaderGroupNodeId: Guid.NewGuid(),
                IsUploaderBranchAdmin: false,
                BranchAdminUserIds: new[] { assignedAdminUserId },
                HigherAdminUserIds: new[] { Guid.NewGuid() },
                MatchKind: "HARD_DUPLICATE",
                ReasonCode: "exact_byte_sha256_size_match",
                DetectedAtUtc: DateTimeOffset.UtcNow),
            CancellationToken.None);
    }

    private async Task<int> CountDecisionRecordsAsync(Guid incidentId)
    {
        using IServiceScope scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        return await db.DuplicateIncidentDecisionRecords.CountAsync(item => item.IncidentId == incidentId);
    }

    private async Task<AuthenticatedAdminClientContext> SignInWithContextAsync(HttpClient client)
    {
        string login = $"s2-37-user-{Guid.NewGuid():N}";
        const string password = "S2-37-test-password!";
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
        Assert.NotNull(signInBody.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signInBody.AccessToken);

        return new AuthenticatedAdminClientContext(signInBody.User.UserId);
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
            DisplayName = "S2-37 Test User",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.AuthUsers.Add(user);
        await dbContext.SaveChangesAsync();
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

        Assert.Fail(
            $"""
            Step: {stepName}
            Expected status: {expectedStatus} ({(int)expectedStatus})
            Actual status: {response.StatusCode} ({(int)response.StatusCode})
            Response body:
            {responseBody}
            """);
    }

    private sealed record AuthenticatedAdminClientContext(Guid UserId);
}