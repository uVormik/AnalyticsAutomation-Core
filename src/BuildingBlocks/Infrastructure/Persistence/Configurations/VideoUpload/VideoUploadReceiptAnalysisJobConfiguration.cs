using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoUpload;

public sealed class VideoUploadReceiptAnalysisJobConfiguration : IEntityTypeConfiguration<VideoUploadReceiptAnalysisJob>
{
    public void Configure(EntityTypeBuilder<VideoUploadReceiptAnalysisJob> builder)
    {
        builder.ToTable("video_upload_receipt_analysis_jobs");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UploadReceiptId).HasColumnName("upload_receipt_id");
        builder.Property(item => item.PreUploadCheckId).HasColumnName("pre_upload_check_id");

        builder.Property(item => item.CommandName)
            .HasColumnName("command_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.EnqueuedAtUtc).HasColumnName("enqueued_at_utc");

        builder.HasIndex(item => item.UploadReceiptId)
            .HasDatabaseName("ix_video_upload_receipt_analysis_jobs_upload_receipt_id");

        builder.HasIndex(item => item.Status)
            .HasDatabaseName("ix_video_upload_receipt_analysis_jobs_status");
    }
}