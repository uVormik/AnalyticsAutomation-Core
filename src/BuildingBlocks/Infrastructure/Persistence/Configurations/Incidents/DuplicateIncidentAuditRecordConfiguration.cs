using BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.Incidents;

public sealed class DuplicateIncidentAuditRecordConfiguration : IEntityTypeConfiguration<DuplicateIncidentAuditRecord>
{
    public void Configure(EntityTypeBuilder<DuplicateIncidentAuditRecord> builder)
    {
        builder.ToTable("duplicate_incident_audit_records");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.IncidentId).HasColumnName("incident_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.Property(item => item.Category)
            .HasColumnName("category")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.Action)
            .HasColumnName("action")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.PayloadJson)
            .HasColumnName("payload_json")
            .HasMaxLength(4096)
            .IsRequired();

        builder.HasIndex(item => item.IncidentId)
            .HasDatabaseName("ix_duplicate_incident_audit_records_incident_id");
    }
}