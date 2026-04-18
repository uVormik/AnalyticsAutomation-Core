using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDownload;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoDownload;

public sealed class VideoDownloadReceiptConfiguration : IEntityTypeConfiguration<VideoDownloadReceipt>
{
    public void Configure(EntityTypeBuilder<VideoDownloadReceipt> builder)
    {
        builder.ToTable("video_download_receipts");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.DownloadIntentId).HasColumnName("download_intent_id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.DownloadedBytes).HasColumnName("downloaded_bytes");
        builder.Property(item => item.DownloadedAtUtc).HasColumnName("downloaded_at_utc");
        builder.Property(item => item.AcceptedAtUtc).HasColumnName("accepted_at_utc");

        builder.Property(item => item.ExternalVideoId)
            .HasColumnName("external_video_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.ClientReceiptKey)
            .HasColumnName("client_receipt_key")
            .HasMaxLength(256);

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(item => item.DownloadIntentId)
            .HasDatabaseName("ix_video_download_receipts_intent_id");

        builder.HasIndex(item => new { item.DownloadIntentId, item.ClientReceiptKey })
            .HasDatabaseName("ix_video_download_receipts_intent_client_key");

        builder.HasIndex(item => new { item.UserId, item.AcceptedAtUtc })
            .HasDatabaseName("ix_video_download_receipts_user_accepted");
    }
}