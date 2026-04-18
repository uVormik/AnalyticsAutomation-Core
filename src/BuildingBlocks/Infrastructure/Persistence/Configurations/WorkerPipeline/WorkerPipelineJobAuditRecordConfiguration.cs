using BuildingBlocks.Infrastructure.Persistence.Entities.WorkerPipeline;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.WorkerPipeline;

public sealed class WorkerPipelineJobAuditRecordConfiguration : IEntityTypeConfiguration<WorkerPipelineJobAuditRecord>
{
    public void Configure(EntityTypeBuilder<WorkerPipelineJobAuditRecord> builder)
    {
        builder.ToTable("worker_pipeline_job_audit_records");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.JobId).HasColumnName("job_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.Property(item => item.Action)
            .HasColumnName("action")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.PayloadJson)
            .HasColumnName("payload_json")
            .HasMaxLength(4096)
            .IsRequired();

        builder.HasIndex(item => item.JobId)
            .HasDatabaseName("ix_worker_pipeline_job_audit_records_job_id");
    }
}