namespace BuildingBlocks.Contracts.Groups;

public sealed record GroupNodeFlatDto(
    Guid GroupNodeId,
    Guid? ParentGroupNodeId,
    string Code,
    string Name,
    int Depth,
    bool IsActive);

public sealed record GroupRoutingResultDto(
    Guid RequestedGroupNodeId,
    Guid ResolvedGroupNodeId,
    IReadOnlyCollection<Guid> ResolvedAdminUserIds,
    bool EscalatedHigher);