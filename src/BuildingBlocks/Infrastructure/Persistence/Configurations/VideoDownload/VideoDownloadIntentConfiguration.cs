using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDownload;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoDownload;

public sealed class VideoDownloadIntentConfiguration : IEntityTypeConfiguration<VideoDownloadIntent>
{
    public void Configure(EntityTypeBuilder<VideoDownloadIntent> builder)
    {
        builder.ToTable("video_download_intents");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.GroupNodeId).HasColumnName("group_node_id");
        builder.Property(item => item.VideoAssetId).HasColumnName("video_asset_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(item => item.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(item => item.ConsumedAtUtc).HasColumnName("consumed_at_utc");

        builder.Property(item => item.ExternalVideoId)
            .HasColumnName("external_video_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.BusinessObjectKey)
            .HasColumnName("business_object_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.SiteProvider)
            .HasColumnName("site_provider")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.DownloadUrl)
            .HasColumnName("download_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(item => item.RejectedReason)
            .HasColumnName("rejected_reason")
            .HasMaxLength(512);

        builder.HasIndex(item => item.Status)
            .HasDatabaseName("ix_video_download_intents_status");

        builder.HasIndex(item => new { item.UserId, item.BusinessObjectKey, item.CreatedAtUtc })
            .HasDatabaseName("ix_video_download_intents_user_business_created");

        builder.HasIndex(item => item.ExternalVideoId)
            .HasDatabaseName("ix_video_download_intents_external_video_id");
    }
}