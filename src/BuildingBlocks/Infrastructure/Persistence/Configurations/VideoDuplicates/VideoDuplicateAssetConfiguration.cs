using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoDuplicates;

public sealed class VideoDuplicateAssetConfiguration : IEntityTypeConfiguration<VideoDuplicateAsset>
{
    public void Configure(EntityTypeBuilder<VideoDuplicateAsset> builder)
    {
        builder.ToTable("video_duplicate_assets");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UploadReceiptId).HasColumnName("upload_receipt_id");
        builder.Property(item => item.PreUploadCheckId).HasColumnName("pre_upload_check_id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.GroupNodeId).HasColumnName("group_node_id");

        builder.Property(item => item.BusinessObjectKey)
            .HasColumnName("business_object_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.ExternalVideoId)
            .HasColumnName("external_video_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(item => item.SizeBytes).HasColumnName("size_bytes");

        builder.Property(item => item.ByteSha256)
            .HasColumnName("byte_sha256")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.UploadedAtUtc).HasColumnName("uploaded_at_utc");
        builder.Property(item => item.RegisteredAtUtc).HasColumnName("registered_at_utc");
        builder.Property(item => item.IsActive).HasColumnName("is_active");

        builder.HasIndex(item => item.UploadReceiptId)
            .IsUnique()
            .HasDatabaseName("ux_video_duplicate_assets_upload_receipt_id");

        builder.HasIndex(item => new { item.ByteSha256, item.SizeBytes })
            .HasDatabaseName("ix_video_duplicate_assets_fingerprint");

        builder.HasIndex(item => item.BusinessObjectKey)
            .HasDatabaseName("ix_video_duplicate_assets_business_object_key");
    }
}