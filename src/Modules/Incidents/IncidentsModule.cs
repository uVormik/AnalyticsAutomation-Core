using System.Text.Json;
using BuildingBlocks.Contracts.Incidents;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Modules.Incidents;

public sealed class IncidentsOptions
{
    public const string SectionName = "Modules:Incidents";

    public bool DuplicateIncidentRoutingEnabled { get; set; } = true;
}

public interface IDuplicateIncidentRoutingService
{
    Task<DuplicateIncidentV1Dto> CreateFromCandidateAsync(
        DuplicateIncidentCreateRequestV1Dto request,
        CancellationToken cancellationToken);

    Task<AssignedDuplicateIncidentsResponseV1Dto> GetAssignedAsync(
        Guid assignedAdminUserId,
        CancellationToken cancellationToken);

    Task<DuplicateIncidentV1Dto> RecordDecisionAsync(
        Guid incidentId,
        DuplicateIncidentDecisionRequestV1Dto request,
        CancellationToken cancellationToken);
}

public sealed class DuplicateIncidentRoutingService(
    PlatformDbContext dbContext,
    IOptions<IncidentsOptions> options,
    ILogger<DuplicateIncidentRoutingService> logger) : IDuplicateIncidentRoutingService
{
    public async Task<DuplicateIncidentV1Dto> CreateFromCandidateAsync(
        DuplicateIncidentCreateRequestV1Dto request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.DuplicateIncidentRoutingEnabled)
        {
            throw new InvalidOperationException("DuplicateIncident routing is disabled.");
        }

        ValidateCreateRequest(request);

        var existing = await dbContext.DuplicateIncidentRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.DuplicateCandidateId == request.DuplicateCandidateId,
                cancellationToken);

        if (existing is not null)
        {
            return await ToDtoAsync(existing, cancellationToken);
        }

        var createdAtUtc = DateTimeOffset.UtcNow;
        var incidentId = Guid.NewGuid();

        var assignedAdmins = request.IsUploaderBranchAdmin
            ? request.HigherAdminUserIds.Distinct().ToArray()
            : request.BranchAdminUserIds.Distinct().ToArray();

        if (assignedAdmins.Length == 0)
        {
            throw new InvalidOperationException("DuplicateIncident routing requires at least one target admin.");
        }

        var assignmentReason = request.IsUploaderBranchAdmin
            ? DuplicateIncidentV1AssignmentReasons.UploaderIsBranchAdminEscalateHigher
            : DuplicateIncidentV1AssignmentReasons.BranchAdminAssignment;

        var incident = new DuplicateIncidentRecord
        {
            Id = incidentId,
            DuplicateCandidateId = request.DuplicateCandidateId,
            SourceVideoAssetId = request.SourceVideoAssetId,
            MatchedVideoAssetId = request.MatchedVideoAssetId,
            UploaderUserId = request.UploaderUserId,
            UploaderGroupNodeId = request.UploaderGroupNodeId,
            Status = DuplicateIncidentV1Statuses.Assigned,
            Severity = request.IsUploaderBranchAdmin
                ? DuplicateIncidentV1Severities.High
                : DuplicateIncidentV1Severities.Medium,
            MatchKind = request.MatchKind.Trim(),
            ReasonCode = request.ReasonCode.Trim(),
            EscalatedHigher = request.IsUploaderBranchAdmin,
            CreatedAtUtc = createdAtUtc
        };

        var assignments = assignedAdmins
            .Select(adminId => new DuplicateIncidentAssignmentRecord
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                AssignedAdminUserId = adminId,
                AssignmentGroupNodeId = request.UploaderGroupNodeId,
                AssignmentReason = assignmentReason,
                IsActive = true,
                AssignedAtUtc = createdAtUtc
            })
            .ToArray();

        var auditPayload = JsonSerializer.Serialize(new
        {
            incident.Id,
            incident.DuplicateCandidateId,
            incident.SourceVideoAssetId,
            incident.MatchedVideoAssetId,
            incident.UploaderUserId,
            incident.UploaderGroupNodeId,
            incident.EscalatedHigher,
            AssignedAdminUserIds = assignedAdmins,
            AssignmentReason = assignmentReason
        });

        var audit = new DuplicateIncidentAuditRecord
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            Category = "duplicate_incident",
            Action = request.IsUploaderBranchAdmin
                ? "duplicate_incident_escalated"
                : "duplicate_incident_assigned",
            PayloadJson = auditPayload,
            CreatedAtUtc = createdAtUtc
        };

        dbContext.DuplicateIncidentRecords.Add(incident);
        dbContext.DuplicateIncidentAssignmentRecords.AddRange(assignments);
        dbContext.DuplicateIncidentAuditRecords.Add(audit);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "DuplicateIncident created. IncidentId={IncidentId} DuplicateCandidateId={DuplicateCandidateId} EscalatedHigher={EscalatedHigher}",
            incident.Id,
            incident.DuplicateCandidateId,
            incident.EscalatedHigher);

        return await ToDtoAsync(incident, cancellationToken);
    }

    public async Task<AssignedDuplicateIncidentsResponseV1Dto> GetAssignedAsync(
        Guid assignedAdminUserId,
        CancellationToken cancellationToken)
    {
        if (assignedAdminUserId == Guid.Empty)
        {
            throw new ArgumentException("AssignedAdminUserId is required.", nameof(assignedAdminUserId));
        }

        var incidentIds = await dbContext.DuplicateIncidentAssignmentRecords
            .AsNoTracking()
            .Where(item => item.AssignedAdminUserId == assignedAdminUserId && item.IsActive)
            .Select(item => item.IncidentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var incidents = await dbContext.DuplicateIncidentRecords
            .AsNoTracking()
            .Where(item => incidentIds.Contains(item.Id))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<DuplicateIncidentV1Dto>();
        foreach (var incident in incidents)
        {
            result.Add(await ToDtoAsync(incident, cancellationToken));
        }

        return new AssignedDuplicateIncidentsResponseV1Dto(
            AssignedAdminUserId: assignedAdminUserId,
            Incidents: result);
    }

    public async Task<DuplicateIncidentV1Dto> RecordDecisionAsync(
        Guid incidentId,
        DuplicateIncidentDecisionRequestV1Dto request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.DuplicateIncidentRoutingEnabled)
        {
            throw new InvalidOperationException("DuplicateIncident routing is disabled.");
        }

        if (incidentId == Guid.Empty)
        {
            throw new ArgumentException("IncidentId is required.", nameof(incidentId));
        }

        ValidateDecisionRequest(request);

        var incident = await dbContext.DuplicateIncidentRecords
            .SingleOrDefaultAsync(
                item => item.Id == incidentId,
                cancellationToken);

        if (incident is null)
        {
            throw new InvalidOperationException("DuplicateIncident was not found.");
        }

        var decidedAtUtc = DateTimeOffset.UtcNow;

        var decision = new DuplicateIncidentDecisionRecord
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

        incident.Status = DuplicateIncidentV1Statuses.Resolved;

        var auditPayload = JsonSerializer.Serialize(new
        {
            IncidentId = incident.Id,
            DecisionId = decision.Id,
            decision.DecidedByUserId,
            decision.Decision,
            decision.Notes
        });

        var audit = new DuplicateIncidentAuditRecord
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            Category = "duplicate_incident",
            Action = "duplicate_incident_decision_recorded",
            PayloadJson = auditPayload,
            CreatedAtUtc = decidedAtUtc
        };

        dbContext.DuplicateIncidentDecisionRecords.Add(decision);
        dbContext.DuplicateIncidentAuditRecords.Add(audit);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "DuplicateIncident decision recorded. IncidentId={IncidentId} Decision={Decision} DecidedByUserId={DecidedByUserId}",
            incidentId,
            decision.Decision,
            decision.DecidedByUserId);

        return await ToDtoAsync(incident, cancellationToken);
    }

    private async Task<DuplicateIncidentV1Dto> ToDtoAsync(
        DuplicateIncidentRecord incident,
        CancellationToken cancellationToken)
    {
        var assignments = await dbContext.DuplicateIncidentAssignmentRecords
            .AsNoTracking()
            .Where(item => item.IncidentId == incident.Id && item.IsActive)
            .OrderBy(item => item.AssignedAtUtc)
            .ToListAsync(cancellationToken);

        var latestDecision = await dbContext.DuplicateIncidentDecisionRecords
            .AsNoTracking()
            .Where(item => item.IncidentId == incident.Id)
            .OrderByDescending(item => item.DecidedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new DuplicateIncidentV1Dto(
            IncidentId: incident.Id,
            DuplicateCandidateId: incident.DuplicateCandidateId,
            SourceVideoAssetId: incident.SourceVideoAssetId,
            MatchedVideoAssetId: incident.MatchedVideoAssetId,
            UploaderUserId: incident.UploaderUserId,
            UploaderGroupNodeId: incident.UploaderGroupNodeId,
            Status: incident.Status,
            Severity: incident.Severity,
            MatchKind: incident.MatchKind,
            ReasonCode: incident.ReasonCode,
            EscalatedHigher: incident.EscalatedHigher,
            Assignments: assignments.Select(ToAssignmentDto).ToArray(),
            LatestDecision: latestDecision is null ? null : ToDecisionDto(latestDecision),
            CreatedAtUtc: incident.CreatedAtUtc);
    }

    private static DuplicateIncidentAssignmentV1Dto ToAssignmentDto(
        DuplicateIncidentAssignmentRecord assignment)
    {
        return new DuplicateIncidentAssignmentV1Dto(
            AssignmentId: assignment.Id,
            AssignedAdminUserId: assignment.AssignedAdminUserId,
            AssignmentGroupNodeId: assignment.AssignmentGroupNodeId,
            AssignmentReason: assignment.AssignmentReason,
            AssignedAtUtc: assignment.AssignedAtUtc);
    }

    private static DuplicateIncidentDecisionV1Dto ToDecisionDto(
        DuplicateIncidentDecisionRecord decision)
    {
        return new DuplicateIncidentDecisionV1Dto(
            DecisionId: decision.Id,
            DecidedByUserId: decision.DecidedByUserId,
            Decision: decision.Decision,
            Notes: decision.Notes,
            DecidedAtUtc: decision.DecidedAtUtc);
    }

    private static void ValidateCreateRequest(DuplicateIncidentCreateRequestV1Dto request)
    {
        if (request.DuplicateCandidateId == Guid.Empty)
        {
            throw new ArgumentException("DuplicateCandidateId is required.", nameof(request));
        }

        if (request.SourceVideoAssetId == Guid.Empty)
        {
            throw new ArgumentException("SourceVideoAssetId is required.", nameof(request));
        }

        if (request.MatchedVideoAssetId == Guid.Empty)
        {
            throw new ArgumentException("MatchedVideoAssetId is required.", nameof(request));
        }

        if (request.UploaderUserId == Guid.Empty)
        {
            throw new ArgumentException("UploaderUserId is required.", nameof(request));
        }

        if (request.UploaderGroupNodeId == Guid.Empty)
        {
            throw new ArgumentException("UploaderGroupNodeId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.MatchKind))
        {
            throw new ArgumentException("MatchKind is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            throw new ArgumentException("ReasonCode is required.", nameof(request));
        }
    }

    private static void ValidateDecisionRequest(DuplicateIncidentDecisionRequestV1Dto request)
    {
        if (request.DecidedByUserId == Guid.Empty)
        {
            throw new ArgumentException("DecidedByUserId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Decision))
        {
            throw new ArgumentException("Decision is required.", nameof(request));
        }

        var allowed = new[]
        {
            DuplicateIncidentV1DecisionTypes.ConfirmDuplicate,
            DuplicateIncidentV1DecisionTypes.DismissDuplicate,
            DuplicateIncidentV1DecisionTypes.Escalate
        };

        if (!allowed.Contains(request.Decision.Trim(), StringComparer.Ordinal))
        {
            throw new ArgumentException("Decision is not supported.", nameof(request));
        }
    }
}

public static class IncidentsModule
{
    public static IServiceCollection AddIncidentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<IncidentsOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection(IncidentsOptions.SectionName);

                if (bool.TryParse(section["DuplicateIncidentRoutingEnabled"], out var enabled))
                {
                    options.DuplicateIncidentRoutingEnabled = enabled;
                }
            })
            .ValidateOnStart();

        services.AddScoped<IDuplicateIncidentRoutingService, DuplicateIncidentRoutingService>();

        return services;
    }

    public static IEndpointRouteBuilder MapIncidentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/incidents/duplicates",
            async Task<IResult> (
                DuplicateIncidentCreateRequestV1Dto request,
                IDuplicateIncidentRoutingService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.CreateFromCandidateAsync(request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "DuplicateIncident routing rejected");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_duplicate_incident_request",
                        message = exception.Message
                    });
                }
            });

        endpoints.MapGet(
            "/api/incidents/duplicates/assigned/{assignedAdminUserId:guid}",
            async Task<IResult> (
                Guid assignedAdminUserId,
                IDuplicateIncidentRoutingService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetAssignedAsync(assignedAdminUserId, cancellationToken);
                return Results.Ok(result);
            });

        endpoints.MapPost(
            "/api/incidents/duplicates/{incidentId:guid}/decision",
            async Task<IResult> (
                Guid incidentId,
                DuplicateIncidentDecisionRequestV1Dto request,
                IDuplicateIncidentRoutingService service,
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
                        title: "DuplicateIncident decision rejected");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_duplicate_incident_decision_request",
                        message = exception.Message
                    });
                }
            });

        return endpoints;
    }
}