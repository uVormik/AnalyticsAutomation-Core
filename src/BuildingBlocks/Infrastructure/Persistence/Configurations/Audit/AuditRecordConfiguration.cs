using BuildingBlocks.Infrastructure.Persistence.Entities.Audit;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.Audit;

public sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("audit_records");

        builder.HasKey(item => item.Id)
            .HasName("pk_audit_records");

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.Category).HasColumnName("category").HasMaxLength(128).IsRequired();
        builder.Property(item => item.Action).HasColumnName("action").HasMaxLength(128).IsRequired();
        builder.Property(item => item.SubjectUserId).HasColumnName("subject_user_id");
        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.EntityType).HasColumnName("entity_type").HasMaxLength(128);
        builder.Property(item => item.EntityId).HasColumnName("entity_id").HasMaxLength(128);
        builder.Property(item => item.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        builder.Property(item => item.RequestPath).HasColumnName("request_path").HasMaxLength(256);
        builder.Property(item => item.PayloadJson).HasColumnName("payload_json");
        builder.Property(item => item.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();

        builder.HasIndex(item => item.OccurredAtUtc)
            .HasDatabaseName("ix_audit_records_occurred_at_utc");

        builder.HasIndex(item => new { item.Category, item.Action })
            .HasDatabaseName("ix_audit_records_category_action");

        builder.HasIndex(item => item.SubjectUserId)
            .HasDatabaseName("ix_audit_records_subject_user_id");
    }
}