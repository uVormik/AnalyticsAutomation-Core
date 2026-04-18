using System;
using System.Collections.Generic;
namespace BuildingBlocks.Contracts.Incidents;

public static class DuplicateIncidentV1Statuses
{
    public const string Assigned = "ASSIGNED";
    public const string Resolved = "RESOLVED";
}

public static class DuplicateIncidentV1Severities
{
    public const string Medium = "MEDIUM";
    public const string High = "HIGH";
}

public static class DuplicateIncidentV1AssignmentReasons
{
    public const string BranchAdminAssignment = "BRANCH_ADMIN_ASSIGNMENT";
    public const string UploaderIsBranchAdminEscalateHigher = "UPLOADER_IS_BRANCH_ADMIN_ESCALATE_HIGHER";
}

public static class DuplicateIncidentV1DecisionTypes
{
    public const string ConfirmDuplicate = "CONFIRM_DUPLICATE";
    public const string DismissDuplicate = "DISMISS_DUPLICATE";
    public const string Escalate = "ESCALATE";
}

public sealed record DuplicateIncidentCreateRequestV1Dto(
    Guid DuplicateCandidateId,
    Guid SourceVideoAssetId,
    Guid MatchedVideoAssetId,
    Guid UploaderUserId,
    Guid UploaderGroupNodeId,
    bool IsUploaderBranchAdmin,
    IReadOnlyCollection<Guid> BranchAdminUserIds,
    IReadOnlyCollection<Guid> HigherAdminUserIds,
    string MatchKind,
    string ReasonCode,
    DateTimeOffset DetectedAtUtc);

public sealed record DuplicateIncidentAssignmentV1Dto(
    Guid AssignmentId,
    Guid AssignedAdminUserId,
    Guid AssignmentGroupNodeId,
    string AssignmentReason,
    DateTimeOffset AssignedAtUtc);

public sealed record DuplicateIncidentDecisionV1Dto(
    Guid DecisionId,
    Guid DecidedByUserId,
    string Decision,
    string? Notes,
    DateTimeOffset DecidedAtUtc);

public sealed record DuplicateIncidentV1Dto(
    Guid IncidentId,
    Guid DuplicateCandidateId,
    Guid SourceVideoAssetId,
    Guid MatchedVideoAssetId,
    Guid UploaderUserId,
    Guid UploaderGroupNodeId,
    string Status,
    string Severity,
    string MatchKind,
    string ReasonCode,
    bool EscalatedHigher,
    IReadOnlyCollection<DuplicateIncidentAssignmentV1Dto> Assignments,
    DuplicateIncidentDecisionV1Dto? LatestDecision,
    DateTimeOffset CreatedAtUtc);

public sealed record DuplicateIncidentDecisionRequestV1Dto(
    Guid DecidedByUserId,
    string Decision,
    string? Notes);

public sealed record AssignedDuplicateIncidentsResponseV1Dto(
    Guid AssignedAdminUserId,
    IReadOnlyCollection<DuplicateIncidentV1Dto> Incidents);