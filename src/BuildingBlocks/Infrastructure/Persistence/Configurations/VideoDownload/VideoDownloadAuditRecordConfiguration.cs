using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDownload;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoDownload;

public sealed class VideoDownloadAuditRecordConfiguration : IEntityTypeConfiguration<VideoDownloadAuditRecord>
{
    public void Configure(EntityTypeBuilder<VideoDownloadAuditRecord> builder)
    {
        builder.ToTable("video_download_audit_records");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.DownloadIntentId).HasColumnName("download_intent_id");
        builder.Property(item => item.DownloadReceiptId).HasColumnName("download_receipt_id");
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

        builder.HasIndex(item => item.DownloadIntentId)
            .HasDatabaseName("ix_video_download_audit_records_intent_id");

        builder.HasIndex(item => item.DownloadReceiptId)
            .HasDatabaseName("ix_video_download_audit_records_receipt_id");
    }
}