using BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.FraudSignals;

public sealed class FraudSignalRecordConfiguration : IEntityTypeConfiguration<FraudSignalRecord>
{
    public void Configure(EntityTypeBuilder<FraudSignalRecord> builder)
    {
        builder.ToTable("fraud_signal_records");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UploadReceiptId).HasColumnName("upload_receipt_id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.UploaderGroupNodeId).HasColumnName("uploader_group_node_id");
        builder.Property(item => item.DuplicateCandidateId).HasColumnName("duplicate_candidate_id");
        builder.Property(item => item.SourceVideoAssetId).HasColumnName("source_video_asset_id");
        builder.Property(item => item.MatchedVideoAssetId).HasColumnName("matched_video_asset_id");
        builder.Property(item => item.AttemptsInWindow).HasColumnName("attempts_in_window");
        builder.Property(item => item.DuplicateCandidatesInWindow).HasColumnName("duplicate_candidates_in_window");
        builder.Property(item => item.Score).HasColumnName("score");
        builder.Property(item => item.ObservedAtUtc).HasColumnName("observed_at_utc");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.Property(item => item.BusinessObjectKey)
            .HasColumnName("business_object_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.SignalType)
            .HasColumnName("signal_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.Severity)
            .HasColumnName("severity")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(item => item.UploadReceiptId)
            .IsUnique()
            .HasDatabaseName("ux_fraud_signal_records_upload_receipt_id");

        builder.HasIndex(item => new { item.UserId, item.BusinessObjectKey, item.ObservedAtUtc })
            .HasDatabaseName("ix_fraud_signal_records_user_business_observed");

        builder.HasIndex(item => item.DuplicateCandidateId)
            .HasDatabaseName("ix_fraud_signal_records_duplicate_candidate_id");
    }
}