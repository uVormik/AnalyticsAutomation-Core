using BuildingBlocks.Infrastructure.Persistence.Entities.WorkerPipeline;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.WorkerPipeline;

public sealed class WorkerPipelineJobRecordConfiguration : IEntityTypeConfiguration<WorkerPipelineJobRecord>
{
    public void Configure(EntityTypeBuilder<WorkerPipelineJobRecord> builder)
    {
        builder.ToTable("worker_pipeline_jobs");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UploadReceiptId).HasColumnName("upload_receipt_id");
        builder.Property(item => item.Attempts).HasColumnName("attempts");
        builder.Property(item => item.MaxAttempts).HasColumnName("max_attempts");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(item => item.AvailableAtUtc).HasColumnName("available_at_utc");
        builder.Property(item => item.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(item => item.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(item => item.FailedAtUtc).HasColumnName("failed_at_utc");

        builder.Property(item => item.JobType)
            .HasColumnName("job_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.PayloadJson)
            .HasColumnName("payload_json")
            .HasMaxLength(8192)
            .IsRequired();

        builder.Property(item => item.ResultJson)
            .HasColumnName("result_json")
            .HasMaxLength(8192);

        builder.Property(item => item.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2048);

        builder.HasIndex(item => item.Status)
            .HasDatabaseName("ix_worker_pipeline_jobs_status");

        builder.HasIndex(item => new { item.Status, item.AvailableAtUtc })
            .HasDatabaseName("ix_worker_pipeline_jobs_status_available_at");

        builder.HasIndex(item => new { item.JobType, item.UploadReceiptId })
            .IsUnique()
            .HasDatabaseName("ux_worker_pipeline_jobs_type_upload_receipt");
    }
}