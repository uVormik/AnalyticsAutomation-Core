using System;
using System.Collections.Generic;

namespace BuildingBlocks.Contracts.Groups;

public sealed record GroupNodeDto(
    Guid GroupNodeId,
    Guid? ParentGroupNodeId,
    string Code,
    string Name,
    string Path,
    bool HasChildren,
    IReadOnlyCollection<Guid> AdminUserIds);

public sealed record GroupTreeNodeDto(
    Guid GroupNodeId,
    Guid? ParentGroupNodeId,
    string Code,
    string Name,
    int Level,
    string Path,
    bool IsCurrent,
    bool HasChildren);