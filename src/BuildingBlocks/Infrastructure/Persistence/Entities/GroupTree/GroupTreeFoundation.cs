namespace BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

public sealed class GroupNode
{
    public Guid Id { get; set; }
    public Guid? ParentNodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Depth { get; set; }
    public bool IsActive { get; set; }

    public GroupNode? ParentNode { get; set; }
    public ICollection<GroupNode> Children { get; set; } = new List<GroupNode>();
    public ICollection<GroupAdminAssignment> AdminAssignments { get; set; } = new List<GroupAdminAssignment>();
}

public sealed class GroupAdminAssignment
{
    public Guid GroupNodeId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }

    public GroupNode GroupNode { get; set; } = null!;
}