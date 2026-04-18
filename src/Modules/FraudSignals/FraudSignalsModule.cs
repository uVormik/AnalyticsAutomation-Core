using System.Text.Json;

using BuildingBlocks.Contracts.Incidents;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.FraudSignals;

public sealed class FraudSignalsOptions
{
    public const string SectionName = "Modules:FraudSignals";

    public bool Enabled { get; set; } = true;
    public int MinimumIncidentScore { get; set; } = 70;
    public int RepeatedAttemptThreshold { get; set; } = 3;
    public int DuplicateCandidateThreshold { get; set; } = 1;
}

public interface IFraudSignalService
{
    Task<FraudSignalEvaluationResponseV1Dto> EvaluateAsync(
        FraudSignalObservationV1Dto request,
        CancellationToken cancellationToken);

    Task<AssignedFraudSuspicionIncidentsResponseV1Dto> GetAssignedAsync(
        Guid assignedAdminUserId,
        CancellationToken cancellationToken);

    Task<FraudSuspicionIncidentV1Dto> RecordDecisionAsync(
        Guid incidentId,
        FraudSuspicionIncidentDecisionRequestV1Dto request,
        CancellationToken cancellationToken);
}

public sealed class FraudSignalService(
    PlatformDbContext dbContext,
    IOptions<FraudSignalsOptions> options,
    ILogger<FraudSignalService> logger) : IFraudSignalService
{
    public async Task<FraudSignalEvaluationResponseV1Dto> EvaluateAsync(
        FraudSignalObservationV1Dto request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            throw new InvalidOperationException("FraudSignals module is disabled.");
        }

        ValidateObservation(request);

        var existingSignal = await dbContext.FraudSignalRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UploadReceiptId == request.UploadReceiptId,
                cancellationToken);

        if (existingSignal is not null)
        {
            var existingIncident = await dbContext.FraudSuspicionIncidentRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.SignalId == existingSignal.Id,
                    cancellationToken);

            return new FraudSignalEvaluationResponseV1Dto(
                IncidentCreated: existingIncident is not null,
                Signal: ToSignalDto(existingSignal),
                Incident: existingIncident is null
                    ? null
                    : await ToIncidentDtoAsync(existingIncident, cancellationToken));
        }

        var now = DateTimeOffset.UtcNow;
        var score = CalculateScore(request, options.Value);
        var severity = CalculateSeverity(score);
        var reasonCode = CalculateReasonCode(request, score, options.Value);
        var signalId = Guid.NewGuid();

        var signal = new FraudSignalRecord
        {
            Id = signalId,
            UploadReceiptId = request.UploadReceiptId,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            UploaderGroupNodeId = request.UploaderGroupNodeId,
            DuplicateCandidateId = request.DuplicateCandidateId,
            SourceVideoAssetId = request.SourceVideoAssetId,
            MatchedVideoAssetId = request.MatchedVideoAssetId,
            BusinessObjectKey = request.BusinessObjectKey.Trim(),
            SignalType = request.SignalType.Trim(),
            Severity = severity,
            AttemptsInWindow = request.AttemptsInWindow,
            DuplicateCandidatesInWindow = request.DuplicateCandidatesInWindow,
            Score = score,
            ReasonCode = reasonCode,
            ObservedAtUtc = request.ObservedAtUtc,
            CreatedAtUtc = now
        };

        dbContext.FraudSignalRecords.Add(signal);

        var signalAudit = new FraudSignalAuditRecord
        {
            Id = Guid.NewGuid(),
            SignalId = signalId,
            IncidentId = null,
            Category = "fraud_signal",
            Action = score >= options.Value.MinimumIncidentScore
                ? "fraud_signal_detected"
                : "fraud_signal_ignored_below_threshold",
            PayloadJson = JsonSerializer.Serialize(new
            {
                SignalId = signal.Id,
                signal.UploadReceiptId,
                signal.UserId,
                signal.DeviceId,
                signal.UploaderGroupNodeId,
                signal.BusinessObjectKey,
                signal.SignalType,
                signal.Score,
                signal.Severity,
                signal.ReasonCode
            }),
            CreatedAtUtc = now
        };

        dbContext.FraudSignalAuditRecords.Add(signalAudit);

        FraudSuspicionIncidentRecord? incident = null;

        if (score >= options.Value.MinimumIncidentScore)
        {
            incident = CreateIncident(request, signal, now);
            dbContext.FraudSuspicionIncidentRecords.Add(incident);

            var assignments = CreateAssignments(request, incident, now);
            dbContext.FraudSuspicionIncidentAssignmentRecords.AddRange(assignments);

            var incidentAudit = new FraudSignalAuditRecord
            {
                Id = Guid.NewGuid(),
                SignalId = signalId,
                IncidentId = incident.Id,
                Category = "fraud_suspicion_incident",
                Action = incident.EscalatedHigher
                    ? "fraud_suspicion_escalated"
                    : "fraud_suspicion_assigned",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    IncidentId = incident.Id,
                    SignalId = signal.Id,
                    incident.UserId,
                    incident.UploaderGroupNodeId,
                    incident.Score,
                    incident.Severity,
                    incident.EscalatedHigher,
                    AssignedAdminUserIds = assignments.Select(item => item.AssignedAdminUserId).ToArray()
                }),
                CreatedAtUtc = now
            };

            dbContext.FraudSignalAuditRecords.Add(incidentAudit);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fraud signal evaluated. SignalId={SignalId} Score={Score} IncidentCreated={IncidentCreated}",
            signal.Id,
            signal.Score,
            incident is not null);

        return new FraudSignalEvaluationResponseV1Dto(
            IncidentCreated: incident is not null,
            Signal: ToSignalDto(signal),
            Incident: incident is null
                ? null
                : await ToIncidentDtoAsync(incident, cancellationToken));
    }

    public async Task<AssignedFraudSuspicionIncidentsResponseV1Dto> GetAssignedAsync(
        Guid assignedAdminUserId,
        CancellationToken cancellationToken)
    {
        if (assignedAdminUserId == Guid.Empty)
        {
            throw new ArgumentException("AssignedAdminUserId is required.", nameof(assignedAdminUserId));
        }

        var incidentIds = await dbContext.FraudSuspicionIncidentAssignmentRecords
            .AsNoTracking()
            .Where(item => item.AssignedAdminUserId == assignedAdminUserId && item.IsActive)
            .Select(item => item.IncidentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var incidents = await dbContext.FraudSuspicionIncidentRecords
            .AsNoTracking()
            .Where(item => incidentIds.Contains(item.Id))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<FraudSuspicionIncidentV1Dto>();
        foreach (var incident in incidents)
        {
            result.Add(await ToIncidentDtoAsync(incident, cancellationToken));
        }

        return new AssignedFraudSuspicionIncidentsResponseV1Dto(
            AssignedAdminUserId: assignedAdminUserId,
            Incidents: result);
    }

    public async Task<FraudSuspicionIncidentV1Dto> RecordDecisionAsync(
        Guid incidentId,
        FraudSuspicionIncidentDecisionRequestV1Dto request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            throw new InvalidOperationException("FraudSignals module is disabled.");
        }

        if (incidentId == Guid.Empty)
        {
            throw new ArgumentException("IncidentId is required.", nameof(incidentId));
        }

        ValidateDecisionRequest(request);

        var incident = await dbContext.FraudSuspicionIncidentRecords
            .SingleOrDefaultAsync(
                item => item.Id == incidentId,
                cancellationToken);

        if (incident is null)
        {
            throw new InvalidOperationException("FraudSuspicionIncident was not found.");
        }

        var decidedAtUtc = DateTimeOffset.UtcNow;

        var decision = new FraudSuspicionIncidentDecisionRecord
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            DecidedByUserId = request.DecidedByUserId,
            Decision = request.Decision.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim(),
            DecidedAtUtc = decidedAtUtc
        };

        incident.Status = FraudSuspicionIncidentV1Statuses.Resolved;

        dbContext.FraudSuspicionIncidentDecisionRecords.Add(decision);
        dbContext.FraudSignalAuditRecords.Add(new FraudSignalAuditRecord
        {
            Id = Guid.NewGuid(),
            SignalId = incident.SignalId,
            IncidentId = incident.Id,
            Category = "fraud_suspicion_incident",
            Action = "fraud_suspicion_decision_recorded",
            PayloadJson = JsonSerializer.Serialize(new
            {
                IncidentId = incident.Id,
                DecisionId = decision.Id,
                decision.DecidedByUserId,
                decision.Decision,
                decision.Notes
            }),
            CreatedAtUtc = decidedAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FraudSuspicionIncident decision recorded. IncidentId={IncidentId} Decision={Decision}",
            incidentId,
            decision.Decision);

        return await ToIncidentDtoAsync(incident, cancellationToken);
    }

    private static int CalculateScore(
        FraudSignalObservationV1Dto request,
        FraudSignalsOptions options)
    {
        var score = 0;

        if (request.AttemptsInWindow >= options.RepeatedAttemptThreshold)
        {
            score += 45;
        }

        if (request.DuplicateCandidatesInWindow >= options.DuplicateCandidateThreshold)
        {
            score += 35;
        }

        if (request.DuplicateCandidateId.HasValue)
        {
            score += 20;
        }

        if (request.IsUploaderBranchAdmin)
        {
            score += 10;
        }

        return Math.Min(score, 100);
    }

    private static string CalculateSeverity(int score)
    {
        if (score >= 90)
        {
            return FraudSuspicionIncidentV1Severities.High;
        }

        if (score >= 70)
        {
            return FraudSuspicionIncidentV1Severities.Medium;
        }

        return FraudSuspicionIncidentV1Severities.Low;
    }

    private static string CalculateReasonCode(
        FraudSignalObservationV1Dto request,
        int score,
        FraudSignalsOptions options)
    {
        if (score < options.MinimumIncidentScore)
        {
            return "below_threshold";
        }

        if (request.AttemptsInWindow >= options.RepeatedAttemptThreshold &&
            request.DuplicateCandidatesInWindow >= options.DuplicateCandidateThreshold)
        {
            return "repeated_attempts_with_duplicate_candidates";
        }

        if (request.DuplicateCandidateId.HasValue)
        {
            return "duplicate_candidate_with_behavioral_context";
        }

        return "behavioral_threshold_exceeded";
    }

    private static FraudSuspicionIncidentRecord CreateIncident(
        FraudSignalObservationV1Dto request,
        FraudSignalRecord signal,
        DateTimeOffset createdAtUtc)
    {
        return new FraudSuspicionIncidentRecord
        {
            Id = Guid.NewGuid(),
            SignalId = signal.Id,
            UploadReceiptId = request.UploadReceiptId,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            UploaderGroupNodeId = request.UploaderGroupNodeId,
            DuplicateCandidateId = request.DuplicateCandidateId,
            BusinessObjectKey = request.BusinessObjectKey.Trim(),
            Status = FraudSuspicionIncidentV1Statuses.Assigned,
            Severity = signal.Severity,
            Score = signal.Score,
            ReasonCode = signal.ReasonCode,
            EscalatedHigher = request.IsUploaderBranchAdmin,
            CreatedAtUtc = createdAtUtc
        };
    }

    private static FraudSuspicionIncidentAssignmentRecord[] CreateAssignments(
        FraudSignalObservationV1Dto request,
        FraudSuspicionIncidentRecord incident,
        DateTimeOffset assignedAtUtc)
    {
        var assignedAdmins = request.IsUploaderBranchAdmin
            ? request.HigherAdminUserIds.Distinct().ToArray()
            : request.BranchAdminUserIds.Distinct().ToArray();

        if (assignedAdmins.Length == 0)
        {
            throw new InvalidOperationException("FraudSuspicionIncident routing requires at least one target admin.");
        }

        var reason = request.IsUploaderBranchAdmin
            ? FraudSuspicionIncidentV1AssignmentReasons.UploaderIsBranchAdminEscalateHigher
            : FraudSuspicionIncidentV1AssignmentReasons.BranchAdminAssignment;

        return assignedAdmins
            .Select(adminId => new FraudSuspicionIncidentAssignmentRecord
            {
                Id = Guid.NewGuid(),
                IncidentId = incident.Id,
                AssignedAdminUserId = adminId,
                AssignmentGroupNodeId = request.UploaderGroupNodeId,
                AssignmentReason = reason,
                IsActive = true,
                AssignedAtUtc = assignedAtUtc
            })
            .ToArray();
    }

    private async Task<FraudSuspicionIncidentV1Dto> ToIncidentDtoAsync(
        FraudSuspicionIncidentRecord incident,
        CancellationToken cancellationToken)
    {
        var assignments = await dbContext.FraudSuspicionIncidentAssignmentRecords
            .AsNoTracking()
            .Where(item => item.IncidentId == incident.Id && item.IsActive)
            .OrderBy(item => item.AssignedAtUtc)
            .ToListAsync(cancellationToken);

        var latestDecision = await dbContext.FraudSuspicionIncidentDecisionRecords
            .AsNoTracking()
            .Where(item => item.IncidentId == incident.Id)
            .OrderByDescending(item => item.DecidedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new FraudSuspicionIncidentV1Dto(
            IncidentId: incident.Id,
            SignalId: incident.SignalId,
            UploadReceiptId: incident.UploadReceiptId,
            UserId: incident.UserId,
            DeviceId: incident.DeviceId,
            UploaderGroupNodeId: incident.UploaderGroupNodeId,
            DuplicateCandidateId: incident.DuplicateCandidateId,
            BusinessObjectKey: incident.BusinessObjectKey,
            Status: incident.Status,
            Severity: incident.Severity,
            Score: incident.Score,
            ReasonCode: incident.ReasonCode,
            EscalatedHigher: incident.EscalatedHigher,
            Assignments: assignments.Select(ToAssignmentDto).ToArray(),
            LatestDecision: latestDecision is null ? null : ToDecisionDto(latestDecision),
            CreatedAtUtc: incident.CreatedAtUtc);
    }

    private static FraudSignalSummaryV1Dto ToSignalDto(FraudSignalRecord signal)
    {
        return new FraudSignalSummaryV1Dto(
            SignalId: signal.Id,
            SignalType: signal.SignalType,
            Severity: signal.Severity,
            Score: signal.Score,
            ReasonCode: signal.ReasonCode,
            ObservedAtUtc: signal.ObservedAtUtc);
    }

    private static FraudSuspicionIncidentAssignmentV1Dto ToAssignmentDto(
        FraudSuspicionIncidentAssignmentRecord assignment)
    {
        return new FraudSuspicionIncidentAssignmentV1Dto(
            AssignmentId: assignment.Id,
            AssignedAdminUserId: assignment.AssignedAdminUserId,
            AssignmentGroupNodeId: assignment.AssignmentGroupNodeId,
            AssignmentReason: assignment.AssignmentReason,
            AssignedAtUtc: assignment.AssignedAtUtc);
    }

    private static FraudSuspicionIncidentDecisionV1Dto ToDecisionDto(
        FraudSuspicionIncidentDecisionRecord decision)
    {
        return new FraudSuspicionIncidentDecisionV1Dto(
            DecisionId: decision.Id,
            DecidedByUserId: decision.DecidedByUserId,
            Decision: decision.Decision,
            Notes: decision.Notes,
            DecidedAtUtc: decision.DecidedAtUtc);
    }

    private static void ValidateObservation(FraudSignalObservationV1Dto request)
    {
        if (request.UploadReceiptId == Guid.Empty)
        {
            throw new ArgumentException("UploadReceiptId is required.", nameof(request));
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (request.UploaderGroupNodeId == Guid.Empty)
        {
            throw new ArgumentException("UploaderGroupNodeId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.BusinessObjectKey))
        {
            throw new ArgumentException("BusinessObjectKey is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SignalType))
        {
            throw new ArgumentException("SignalType is required.", nameof(request));
        }

        if (request.AttemptsInWindow < 0)
        {
            throw new ArgumentException("AttemptsInWindow cannot be negative.", nameof(request));
        }

        if (request.DuplicateCandidatesInWindow < 0)
        {
            throw new ArgumentException("DuplicateCandidatesInWindow cannot be negative.", nameof(request));
        }
    }

    private static void ValidateDecisionRequest(FraudSuspicionIncidentDecisionRequestV1Dto request)
    {
        if (request.DecidedByUserId == Guid.Empty)
        {
            throw new ArgumentException("DecidedByUserId is required.", nameof(request));
        }

        var decision = request.Decision.Trim();
        var allowed = new[]
        {
            FraudSuspicionIncidentV1DecisionTypes.ConfirmSuspicion,
            FraudSuspicionIncidentV1DecisionTypes.DismissSuspicion,
            FraudSuspicionIncidentV1DecisionTypes.Escalate
        };

        if (!allowed.Contains(decision, StringComparer.Ordinal))
        {
            throw new ArgumentException("Decision is not supported.", nameof(request));
        }
    }
}

public static class FraudSignalsModule
{
    public static IServiceCollection AddFraudSignalsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FraudSignalsOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection(FraudSignalsOptions.SectionName);

                if (bool.TryParse(section["Enabled"], out var enabled))
                {
                    options.Enabled = enabled;
                }

                if (int.TryParse(section["MinimumIncidentScore"], out var minimumIncidentScore))
                {
                    options.MinimumIncidentScore = minimumIncidentScore;
                }

                if (int.TryParse(section["RepeatedAttemptThreshold"], out var repeatedAttemptThreshold))
                {
                    options.RepeatedAttemptThreshold = repeatedAttemptThreshold;
                }

                if (int.TryParse(section["DuplicateCandidateThreshold"], out var duplicateCandidateThreshold))
                {
                    options.DuplicateCandidateThreshold = duplicateCandidateThreshold;
                }
            })
            .Validate(options => options.MinimumIncidentScore is >= 1 and <= 100, "MinimumIncidentScore must be between 1 and 100.")
            .Validate(options => options.RepeatedAttemptThreshold > 0, "RepeatedAttemptThreshold must be positive.")
            .Validate(options => options.DuplicateCandidateThreshold > 0, "DuplicateCandidateThreshold must be positive.")
            .ValidateOnStart();

        services.AddScoped<IFraudSignalService, FraudSignalService>();

        return services;
    }

    public static IEndpointRouteBuilder MapFraudSignalsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/fraud-signals/evaluate-upload",
            async Task<IResult> (
                FraudSignalObservationV1Dto request,
                IFraudSignalService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.EvaluateAsync(request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "FraudSignals evaluation rejected");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_fraud_signal_observation",
                        message = exception.Message
                    });
                }
            });

        endpoints.MapGet(
            "/api/fraud-signals/incidents/assigned/{assignedAdminUserId:guid}",
            async Task<IResult> (
                Guid assignedAdminUserId,
                IFraudSignalService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetAssignedAsync(assignedAdminUserId, cancellationToken);
                return Results.Ok(result);
            });

        endpoints.MapPost(
            "/api/fraud-signals/incidents/{incidentId:guid}/decision",
            async Task<IResult> (
                Guid incidentId,
                FraudSuspicionIncidentDecisionRequestV1Dto request,
                IFraudSignalService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.RecordDecisionAsync(incidentId, request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "FraudSuspicionIncident decision rejected");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_fraud_suspicion_decision",
                        message = exception.Message
                    });
                }
            });

        return endpoints;
    }
}