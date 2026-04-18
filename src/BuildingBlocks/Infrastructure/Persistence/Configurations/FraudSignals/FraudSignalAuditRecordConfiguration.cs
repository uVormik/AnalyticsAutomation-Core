using BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.FraudSignals;

public sealed class FraudSignalAuditRecordConfiguration : IEntityTypeConfiguration<FraudSignalAuditRecord>
{
    public void Configure(EntityTypeBuilder<FraudSignalAuditRecord> builder)
    {
        builder.ToTable("fraud_signal_audit_records");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.SignalId).HasColumnName("signal_id");
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

        builder.HasIndex(item => item.SignalId)
            .HasDatabaseName("ix_fraud_signal_audit_records_signal_id");

        builder.HasIndex(item => item.IncidentId)
            .HasDatabaseName("ix_fraud_signal_audit_records_incident_id");
    }
}