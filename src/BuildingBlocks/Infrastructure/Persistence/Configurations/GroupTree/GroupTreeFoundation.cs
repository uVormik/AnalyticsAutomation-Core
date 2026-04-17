using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.GroupTree;

internal static class GroupTreeSeedData
{
    public static readonly Guid RootNodeId = Guid.Parse("C15EE7FE-7C9A-4B31-8B7E-C278B59F318C");
    public static readonly Guid BranchANodeId = Guid.Parse("D4D74008-0ED5-4E46-B0A1-91E0628079C0");
    public static readonly Guid BranchASubNodeId = Guid.Parse("D6370F1A-69C0-4D7F-8AF5-81CF7D665E92");
}

public sealed class GroupNodeConfiguration : IEntityTypeConfiguration<GroupNode>
{
    public void Configure(EntityTypeBuilder<GroupNode> builder)
    {
        builder.ToTable("group_nodes");

        builder.HasKey(item => item.Id).HasName("pk_group_nodes");

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.ParentNodeId).HasColumnName("parent_node_id");
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(item => item.Depth).HasColumnName("depth").IsRequired();
        builder.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();

        builder.HasIndex(item => item.Code)
            .HasDatabaseName("ux_group_nodes_code")
            .IsUnique();

        builder.HasOne(item => item.ParentNode)
            .WithMany(item => item.Children)
            .HasForeignKey(item => item.ParentNodeId)
            .HasConstraintName("fk_group_nodes_parent_node_id");

        builder.HasData(
            new GroupNode
            {
                Id = GroupTreeSeedData.RootNodeId,
                ParentNodeId = null,
                Code = "root",
                Name = "Root",
                Depth = 0,
                IsActive = true
            },
            new GroupNode
            {
                Id = GroupTreeSeedData.BranchANodeId,
                ParentNodeId = GroupTreeSeedData.RootNodeId,
                Code = "branch-a",
                Name = "Branch A",
                Depth = 1,
                IsActive = true
            },
            new GroupNode
            {
                Id = GroupTreeSeedData.BranchASubNodeId,
                ParentNodeId = GroupTreeSeedData.BranchANodeId,
                Code = "branch-a-sub",
                Name = "Branch A Sub",
                Depth = 2,
                IsActive = true
            });
    }
}

public sealed class GroupAdminAssignmentConfiguration : IEntityTypeConfiguration<GroupAdminAssignment>
{
    public void Configure(EntityTypeBuilder<GroupAdminAssignment> builder)
    {
        builder.ToTable("group_admin_assignments");

        builder.HasKey(item => new { item.GroupNodeId, item.UserId })
            .HasName("pk_group_admin_assignments");

        builder.Property(item => item.GroupNodeId).HasColumnName("group_node_id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.AssignedAtUtc).HasColumnName("assigned_at_utc").IsRequired();

        builder.HasOne(item => item.GroupNode)
            .WithMany(item => item.AdminAssignments)
            .HasForeignKey(item => item.GroupNodeId)
            .HasConstraintName("fk_group_admin_assignments_group_nodes_group_node_id");

        builder.HasOne<AuthUser>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_group_admin_assignments_auth_users_user_id");
    }
}