using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoUpload;

public sealed class VideoUploadReceiptConfiguration : IEntityTypeConfiguration<VideoUploadReceipt>
{
    public void Configure(EntityTypeBuilder<VideoUploadReceipt> builder)
    {
        builder.ToTable("video_upload_receipts");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.PreUploadCheckId).HasColumnName("pre_upload_check_id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.GroupNodeId).HasColumnName("group_node_id");

        builder.Property(item => item.ExternalVideoId)
            .HasColumnName("external_video_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(item => item.SiteStatus)
            .HasColumnName("site_status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.SizeBytes).HasColumnName("size_bytes");

        builder.Property(item => item.ByteSha256)
            .HasColumnName("byte_sha256")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.ReceiptStatus)
            .HasColumnName("receipt_status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.AnalysisJobStatus)
            .HasColumnName("analysis_job_status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.UploadedAtUtc).HasColumnName("uploaded_at_utc");
        builder.Property(item => item.ReceivedAtUtc).HasColumnName("received_at_utc");

        builder.HasIndex(item => item.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_video_upload_receipts_idempotency_key");

        builder.HasIndex(item => item.PreUploadCheckId)
            .HasDatabaseName("ix_video_upload_receipts_pre_upload_check_id");

        builder.HasIndex(item => new { item.ByteSha256, item.SizeBytes })
            .HasDatabaseName("ix_video_upload_receipts_fingerprint");
    }
}