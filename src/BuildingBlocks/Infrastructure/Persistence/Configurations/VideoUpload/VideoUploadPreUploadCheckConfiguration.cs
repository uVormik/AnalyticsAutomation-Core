using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoUpload;

public sealed class VideoUploadPreUploadCheckConfiguration : IEntityTypeConfiguration<VideoUploadPreUploadCheck>
{
    public void Configure(EntityTypeBuilder<VideoUploadPreUploadCheck> builder)
    {
        builder.ToTable("video_upload_pre_upload_checks");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.GroupNodeId).HasColumnName("group_node_id");

        builder.Property(item => item.BusinessObjectKey)
            .HasColumnName("business_object_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(item => item.SizeBytes).HasColumnName("size_bytes");

        builder.Property(item => item.ByteSha256)
            .HasColumnName("byte_sha256")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(128);

        builder.Property(item => item.CapturedAtUtc).HasColumnName("captured_at_utc");

        builder.Property(item => item.Decision)
            .HasColumnName("decision")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.CanUploadToSite).HasColumnName("can_upload_to_site");

        builder.Property(item => item.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.ExistingPreUploadCheckId).HasColumnName("existing_pre_upload_check_id");

        builder.Property(item => item.RequiredNextSteps)
            .HasColumnName("required_next_steps")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(item => item.SiteProvider)
            .HasColumnName("site_provider")
            .HasMaxLength(64);

        builder.Property(item => item.ExternalVideoId)
            .HasColumnName("external_video_id")
            .HasMaxLength(256);

        builder.Property(item => item.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(512);

        builder.Property(item => item.CheckedAtUtc).HasColumnName("checked_at_utc");

        builder.HasIndex(item => new { item.ByteSha256, item.SizeBytes })
            .HasDatabaseName("ix_video_upload_pre_upload_checks_fingerprint");

        builder.HasIndex(item => new { item.UserId, item.BusinessObjectKey })
            .HasDatabaseName("ix_video_upload_pre_upload_checks_user_business_object");

        builder.HasIndex(item => item.CheckedAtUtc)
            .HasDatabaseName("ix_video_upload_pre_upload_checks_checked_at_utc");
    }
}