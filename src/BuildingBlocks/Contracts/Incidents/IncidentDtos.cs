using System;

using BuildingBlocks.Contracts.VideoUpload;

namespace BuildingBlocks.Contracts.Incidents;

public enum IncidentType
{
    DuplicateIncident = 0,
    FraudSuspicionIncident = 1
}

public enum IncidentStatus
{
    Open = 0,
    InReview = 1,
    Resolved = 2,
    Rejected = 3
}

public enum IncidentSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public sealed record DuplicateIncidentSummaryDto(
    Guid IncidentId,
    IncidentStatus Status,
    IncidentSeverity Severity,
    DuplicateMatchKind MatchKind,
    Guid BusinessObjectId,
    string BusinessObjectType,
    Guid? VideoAssetId,
    Guid? AssignedGroupNodeId,
    DateTimeOffset CreatedAtUtc,
    string? ReasonCode);

public sealed record FraudSuspicionIncidentSummaryDto(
    Guid IncidentId,
    IncidentStatus Status,
    IncidentSeverity Severity,
    string ReasonCode,
    Guid BusinessObjectId,
    string BusinessObjectType,
    Guid? VideoAssetId,
    Guid? AssignedGroupNodeId,
    DateTimeOffset CreatedAtUtc);