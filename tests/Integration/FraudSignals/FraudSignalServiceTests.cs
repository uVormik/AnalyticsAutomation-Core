using BuildingBlocks.Contracts.Incidents;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.FraudSignals;

namespace FraudSignals.IntegrationTests;

public sealed class FraudSignalServiceTests
{
    [Fact]
    public async Task EvaluateAsyncCreatesIncidentForRepeatedDuplicateSignal()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IFraudSignalService>();
        var branchAdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await service.EvaluateAsync(
            CreateObservation(isUploaderBranchAdmin: false, branchAdminId: branchAdminId),
            CancellationToken.None);

        Assert.True(result.IncidentCreated);
        Assert.NotNull(result.Incident);
        Assert.True(result.Signal.Score >= 70);
        Assert.Equal(FraudSuspicionIncidentV1Statuses.Assigned, result.Incident!.Status);
        Assert.Single(result.Incident.Assignments);
        Assert.Equal(branchAdminId, result.Incident.Assignments.Single().AssignedAdminUserId);
        Assert.Equal(FraudSuspicionIncidentV1AssignmentReasons.BranchAdminAssignment, result.Incident.Assignments.Single().AssignmentReason);
    }

    [Fact]
    public async Task EvaluateAsyncEscalatesBranchAdminUploaderToHigherAdmin()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IFraudSignalService>();
        var higherAdminId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var result = await service.EvaluateAsync(
            CreateObservation(isUploaderBranchAdmin: true, higherAdminId: higherAdminId),
            CancellationToken.None);

        Assert.True(result.IncidentCreated);
        Assert.NotNull(result.Incident);
        Assert.True(result.Incident!.EscalatedHigher);
        Assert.Equal(higherAdminId, result.Incident.Assignments.Single().AssignedAdminUserId);
        Assert.Equal(FraudSuspicionIncidentV1AssignmentReasons.UploaderIsBranchAdminEscalateHigher, result.Incident.Assignments.Single().AssignmentReason);
    }

    [Fact]
    public async Task EvaluateAsyncIgnoresBelowThresholdSignal()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IFraudSignalService>();

        var result = await service.EvaluateAsync(
            CreateObservation(
                isUploaderBranchAdmin: false,
                attemptsInWindow: 1,
                duplicateCandidatesInWindow: 0,
                duplicateCandidateId: null),
            CancellationToken.None);

        Assert.False(result.IncidentCreated);
        Assert.Null(result.Incident);
        Assert.Equal(FraudSuspicionIncidentV1Statuses.IgnoredBelowThreshold, result.Signal.ReasonCode == "below_threshold"
            ? FraudSuspicionIncidentV1Statuses.IgnoredBelowThreshold
            : "unexpected");
    }

    [Fact]
    public async Task EvaluateAsyncIsIdempotentByUploadReceiptId()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IFraudSignalService>();
        var uploadReceiptId = Guid.NewGuid();

        var first = await service.EvaluateAsync(
            CreateObservation(isUploaderBranchAdmin: false, uploadReceiptId: uploadReceiptId),
            CancellationToken.None);

        var second = await service.EvaluateAsync(
            CreateObservation(isUploaderBranchAdmin: false, uploadReceiptId: uploadReceiptId),
            CancellationToken.None);

        Assert.Equal(first.Signal.SignalId, second.Signal.SignalId);
        Assert.Equal(first.Incident?.IncidentId, second.Incident?.IncidentId);
    }

    [Fact]
    public async Task RecordDecisionAsyncResolvesIncident()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IFraudSignalService>();
        var created = await service.EvaluateAsync(
            CreateObservation(isUploaderBranchAdmin: false),
            CancellationToken.None);

        var decided = await service.RecordDecisionAsync(
            created.Incident!.IncidentId,
            new FraudSuspicionIncidentDecisionRequestV1Dto(
                DecidedByUserId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Decision: FraudSuspicionIncidentV1DecisionTypes.ConfirmSuspicion,
                Notes: "Confirmed by test."),
            CancellationToken.None);

        Assert.Equal(FraudSuspicionIncidentV1Statuses.Resolved, decided.Status);
        Assert.NotNull(decided.LatestDecision);
        Assert.Equal(FraudSuspicionIncidentV1DecisionTypes.ConfirmSuspicion, decided.LatestDecision!.Decision);
    }

    private static ServiceProvider CreateProvider()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:FraudSignals:Enabled"] = "true",
            ["Modules:FraudSignals:MinimumIncidentScore"] = "70",
            ["Modules:FraudSignals:RepeatedAttemptThreshold"] = "3",
            ["Modules:FraudSignals:DuplicateCandidateThreshold"] = "1"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PlatformDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddFraudSignalsModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static FraudSignalObservationV1Dto CreateObservation(
        bool isUploaderBranchAdmin,
        Guid? branchAdminId = null,
        Guid? higherAdminId = null,
        Guid? uploadReceiptId = null,
        int attemptsInWindow = 3,
        int duplicateCandidatesInWindow = 1,
        Guid? duplicateCandidateId = null)
    {
        var candidateId = duplicateCandidateId ?? Guid.NewGuid();

        return new FraudSignalObservationV1Dto(
            UploadReceiptId: uploadReceiptId ?? Guid.NewGuid(),
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DeviceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UploaderGroupNodeId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            DuplicateCandidateId: candidateId,
            SourceVideoAssetId: Guid.NewGuid(),
            MatchedVideoAssetId: Guid.NewGuid(),
            BusinessObjectKey: "demo-business-object",
            SignalType: FraudSignalV1Types.RepeatedUploadAttemptsWindow,
            AttemptsInWindow: attemptsInWindow,
            DuplicateCandidatesInWindow: duplicateCandidatesInWindow,
            IsUploaderBranchAdmin: isUploaderBranchAdmin,
            BranchAdminUserIds: new[] { branchAdminId ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
            HigherAdminUserIds: new[] { higherAdminId ?? Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
            ObservedAtUtc: DateTimeOffset.Parse(
                "2026-04-18T00:00:00+00:00",
                System.Globalization.CultureInfo.InvariantCulture));
    }
}