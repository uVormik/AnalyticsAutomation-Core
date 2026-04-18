using BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.Incidents;

public sealed class DuplicateIncidentDecisionRecordConfiguration : IEntityTypeConfiguration<DuplicateIncidentDecisionRecord>
{
    public void Configure(EntityTypeBuilder<DuplicateIncidentDecisionRecord> builder)
    {
        builder.ToTable("duplicate_incident_decisions");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.IncidentId).HasColumnName("incident_id");
        builder.Property(item => item.DecidedByUserId).HasColumnName("decided_by_user_id");
        builder.Property(item => item.DecidedAtUtc).HasColumnName("decided_at_utc");

        builder.Property(item => item.Decision)
            .HasColumnName("decision")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1024);

        builder.HasIndex(item => item.IncidentId)
            .HasDatabaseName("ix_duplicate_incident_decisions_incident_id");
    }
}