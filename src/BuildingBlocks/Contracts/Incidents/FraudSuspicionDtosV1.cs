using System;
using System.Collections.Generic;

namespace BuildingBlocks.Contracts.Incidents;

public static class FraudSignalV1Types
{
    public const string RepeatedUploadAttemptsWindow = "REPEATED_UPLOAD_ATTEMPTS_WINDOW";
    public const string DuplicateAfterPriorReview = "DUPLICATE_AFTER_PRIOR_REVIEW";
}

public static class FraudSuspicionIncidentV1Statuses
{
    public const string Assigned = "ASSIGNED";
    public const string Resolved = "RESOLVED";
    public const string IgnoredBelowThreshold = "IGNORED_BELOW_THRESHOLD";
}

public static class FraudSuspicionIncidentV1Severities
{
    public const string Low = "LOW";
    public const string Medium = "MEDIUM";
    public const string High = "HIGH";
}

public static class FraudSuspicionIncidentV1AssignmentReasons
{
    public const string BranchAdminAssignment = "BRANCH_ADMIN_ASSIGNMENT";
    public const string UploaderIsBranchAdminEscalateHigher = "UPLOADER_IS_BRANCH_ADMIN_ESCALATE_HIGHER";
}

public static class FraudSuspicionIncidentV1DecisionTypes
{
    public const string ConfirmSuspicion = "CONFIRM_SUSPICION";
    public const string DismissSuspicion = "DISMISS_SUSPICION";
    public const string Escalate = "ESCALATE";
}

public sealed record FraudSignalObservationV1Dto(
    Guid UploadReceiptId,
    Guid UserId,
    Guid? DeviceId,
    Guid UploaderGroupNodeId,
    Guid? DuplicateCandidateId,
    Guid? SourceVideoAssetId,
    Guid? MatchedVideoAssetId,
    string BusinessObjectKey,
    string SignalType,
    int AttemptsInWindow,
    int DuplicateCandidatesInWindow,
    bool IsUploaderBranchAdmin,
    IReadOnlyCollection<Guid> BranchAdminUserIds,
    IReadOnlyCollection<Guid> HigherAdminUserIds,
    DateTimeOffset ObservedAtUtc);

public sealed record FraudSignalSummaryV1Dto(
    Guid SignalId,
    string SignalType,
    string Severity,
    int Score,
    string ReasonCode,
    DateTimeOffset ObservedAtUtc);

public sealed record FraudSuspicionIncidentAssignmentV1Dto(
    Guid AssignmentId,
    Guid AssignedAdminUserId,
    Guid AssignmentGroupNodeId,
    string AssignmentReason,
    DateTimeOffset AssignedAtUtc);

public sealed record FraudSuspicionIncidentDecisionV1Dto(
    Guid DecisionId,
    Guid DecidedByUserId,
    string Decision,
    string? Notes,
    DateTimeOffset DecidedAtUtc);

public sealed record FraudSuspicionIncidentV1Dto(
    Guid IncidentId,
    Guid SignalId,
    Guid UploadReceiptId,
    Guid UserId,
    Guid? DeviceId,
    Guid UploaderGroupNodeId,
    Guid? DuplicateCandidateId,
    string BusinessObjectKey,
    string Status,
    string Severity,
    int Score,
    string ReasonCode,
    bool EscalatedHigher,
    IReadOnlyCollection<FraudSuspicionIncidentAssignmentV1Dto> Assignments,
    FraudSuspicionIncidentDecisionV1Dto? LatestDecision,
    DateTimeOffset CreatedAtUtc);

public sealed record FraudSignalEvaluationResponseV1Dto(
    bool IncidentCreated,
    FraudSignalSummaryV1Dto Signal,
    FraudSuspicionIncidentV1Dto? Incident);

public sealed record FraudSuspicionIncidentDecisionRequestV1Dto(
    Guid DecidedByUserId,
    string Decision,
    string? Notes);

public sealed record AssignedFraudSuspicionIncidentsResponseV1Dto(
    Guid AssignedAdminUserId,
    IReadOnlyCollection<FraudSuspicionIncidentV1Dto> Incidents);