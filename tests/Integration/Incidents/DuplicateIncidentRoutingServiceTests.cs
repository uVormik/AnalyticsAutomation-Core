using BuildingBlocks.Contracts.Incidents;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.Incidents;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Incidents.IntegrationTests;

public sealed class DuplicateIncidentRoutingServiceTests
{
    [Fact]
    public async Task CreateFromCandidateAsyncAssignsRegularUploaderToBranchAdmin()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IDuplicateIncidentRoutingService>();
        var branchAdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await service.CreateFromCandidateAsync(
            CreateRequest(isUploaderBranchAdmin: false, branchAdminId: branchAdminId),
            CancellationToken.None);

        Assert.False(result.EscalatedHigher);
        Assert.Equal(DuplicateIncidentV1Statuses.Assigned, result.Status);
        Assert.Single(result.Assignments);
        Assert.Equal(branchAdminId, result.Assignments.Single().AssignedAdminUserId);
        Assert.Equal(DuplicateIncidentV1AssignmentReasons.BranchAdminAssignment, result.Assignments.Single().AssignmentReason);
    }

    [Fact]
    public async Task CreateFromCandidateAsyncEscalatesBranchAdminUploaderToHigherAdmin()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IDuplicateIncidentRoutingService>();
        var higherAdminId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var result = await service.CreateFromCandidateAsync(
            CreateRequest(isUploaderBranchAdmin: true, higherAdminId: higherAdminId),
            CancellationToken.None);

        Assert.True(result.EscalatedHigher);
        Assert.Equal(DuplicateIncidentV1Severities.High, result.Severity);
        Assert.Single(result.Assignments);
        Assert.Equal(higherAdminId, result.Assignments.Single().AssignedAdminUserId);
        Assert.Equal(DuplicateIncidentV1AssignmentReasons.UploaderIsBranchAdminEscalateHigher, result.Assignments.Single().AssignmentReason);
    }

    [Fact]
    public async Task CreateFromCandidateAsyncIsIdempotentByDuplicateCandidateId()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IDuplicateIncidentRoutingService>();
        var request = CreateRequest(isUploaderBranchAdmin: false);

        var first = await service.CreateFromCandidateAsync(request, CancellationToken.None);
        var second = await service.CreateFromCandidateAsync(request, CancellationToken.None);

        Assert.Equal(first.IncidentId, second.IncidentId);
        Assert.Equal(first.Assignments.Single().AssignedAdminUserId, second.Assignments.Single().AssignedAdminUserId);
    }

    [Fact]
    public async Task RecordDecisionAsyncResolvesIncident()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IDuplicateIncidentRoutingService>();
        var created = await service.CreateFromCandidateAsync(
            CreateRequest(isUploaderBranchAdmin: false),
            CancellationToken.None);

        var decided = await service.RecordDecisionAsync(
            created.IncidentId,
            new DuplicateIncidentDecisionRequestV1Dto(
                DecidedByUserId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Decision: DuplicateIncidentV1DecisionTypes.ConfirmDuplicate,
                Notes: "Confirmed by test."),
            CancellationToken.None);

        Assert.Equal(DuplicateIncidentV1Statuses.Resolved, decided.Status);
        Assert.NotNull(decided.LatestDecision);
        Assert.Equal(DuplicateIncidentV1DecisionTypes.ConfirmDuplicate, decided.LatestDecision!.Decision);
    }

    private static ServiceProvider CreateProvider()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:Incidents:DuplicateIncidentRoutingEnabled"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PlatformDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddIncidentsModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static DuplicateIncidentCreateRequestV1Dto CreateRequest(
        bool isUploaderBranchAdmin,
        Guid? branchAdminId = null,
        Guid? higherAdminId = null)
    {
        return new DuplicateIncidentCreateRequestV1Dto(
            DuplicateCandidateId: Guid.NewGuid(),
            SourceVideoAssetId: Guid.NewGuid(),
            MatchedVideoAssetId: Guid.NewGuid(),
            UploaderUserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UploaderGroupNodeId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            IsUploaderBranchAdmin: isUploaderBranchAdmin,
            BranchAdminUserIds: new[] { branchAdminId ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
            HigherAdminUserIds: new[] { higherAdminId ?? Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
            MatchKind: "HARD_DUPLICATE",
            ReasonCode: "exact_byte_sha256_size_match",
            DetectedAtUtc: DateTimeOffset.Parse(
                "2026-04-18T00:00:00+00:00",
                System.Globalization.CultureInfo.InvariantCulture));
    }
}