using BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.Incidents;

public sealed class DuplicateIncidentAssignmentRecordConfiguration : IEntityTypeConfiguration<DuplicateIncidentAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<DuplicateIncidentAssignmentRecord> builder)
    {
        builder.ToTable("duplicate_incident_assignments");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.IncidentId).HasColumnName("incident_id");
        builder.Property(item => item.AssignedAdminUserId).HasColumnName("assigned_admin_user_id");
        builder.Property(item => item.AssignmentGroupNodeId).HasColumnName("assignment_group_node_id");
        builder.Property(item => item.IsActive).HasColumnName("is_active");
        builder.Property(item => item.AssignedAtUtc).HasColumnName("assigned_at_utc");

        builder.Property(item => item.AssignmentReason)
            .HasColumnName("assignment_reason")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(item => item.IncidentId)
            .HasDatabaseName("ix_duplicate_incident_assignments_incident_id");

        builder.HasIndex(item => item.AssignedAdminUserId)
            .HasDatabaseName("ix_duplicate_incident_assignments_admin_user_id");

        builder.HasIndex(item => new { item.IncidentId, item.AssignedAdminUserId })
            .IsUnique()
            .HasDatabaseName("ux_duplicate_incident_assignments_incident_admin");
    }
}