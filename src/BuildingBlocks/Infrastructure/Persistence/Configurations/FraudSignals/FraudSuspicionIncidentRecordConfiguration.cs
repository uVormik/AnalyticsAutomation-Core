using BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.FraudSignals;

public sealed class FraudSuspicionIncidentRecordConfiguration : IEntityTypeConfiguration<FraudSuspicionIncidentRecord>
{
    public void Configure(EntityTypeBuilder<FraudSuspicionIncidentRecord> builder)
    {
        builder.ToTable("fraud_suspicion_incidents");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.SignalId).HasColumnName("signal_id");
        builder.Property(item => item.UploadReceiptId).HasColumnName("upload_receipt_id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.UploaderGroupNodeId).HasColumnName("uploader_group_node_id");
        builder.Property(item => item.DuplicateCandidateId).HasColumnName("duplicate_candidate_id");
        builder.Property(item => item.Score).HasColumnName("score");
        builder.Property(item => item.EscalatedHigher).HasColumnName("escalated_higher");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.Property(item => item.BusinessObjectKey)
            .HasColumnName("business_object_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Severity)
            .HasColumnName("severity")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(item => item.SignalId)
            .IsUnique()
            .HasDatabaseName("ux_fraud_suspicion_incidents_signal_id");

        builder.HasIndex(item => item.Status)
            .HasDatabaseName("ix_fraud_suspicion_incidents_status");

        builder.HasIndex(item => item.UploaderGroupNodeId)
            .HasDatabaseName("ix_fraud_suspicion_incidents_uploader_group_node_id");
    }
}