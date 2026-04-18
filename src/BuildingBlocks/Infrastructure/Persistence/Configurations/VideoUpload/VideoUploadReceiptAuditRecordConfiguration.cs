using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoUpload;

public sealed class VideoUploadReceiptAuditRecordConfiguration : IEntityTypeConfiguration<VideoUploadReceiptAuditRecord>
{
    public void Configure(EntityTypeBuilder<VideoUploadReceiptAuditRecord> builder)
    {
        builder.ToTable("video_upload_receipt_audit_records");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UploadReceiptId).HasColumnName("upload_receipt_id");

        builder.Property(item => item.Category)
            .HasColumnName("category")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.Action)
            .HasColumnName("action")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.PayloadJson)
            .HasColumnName("payload_json")
            .HasMaxLength(4096)
            .IsRequired();

        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.HasIndex(item => item.UploadReceiptId)
            .HasDatabaseName("ix_video_upload_receipt_audit_records_upload_receipt_id");

        builder.HasIndex(item => item.CorrelationId)
            .HasDatabaseName("ix_video_upload_receipt_audit_records_correlation_id");
    }
}