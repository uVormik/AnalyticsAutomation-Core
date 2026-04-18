using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoDuplicates;

public sealed class VideoDuplicateRegistryAuditRecordConfiguration : IEntityTypeConfiguration<VideoDuplicateRegistryAuditRecord>
{
    public void Configure(EntityTypeBuilder<VideoDuplicateRegistryAuditRecord> builder)
    {
        builder.ToTable("video_duplicate_registry_audit_records");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.VideoAssetId).HasColumnName("video_asset_id");
        builder.Property(item => item.DuplicateCandidateId).HasColumnName("duplicate_candidate_id");

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

        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.HasIndex(item => item.VideoAssetId)
            .HasDatabaseName("ix_video_duplicate_registry_audit_records_asset_id");
    }
}