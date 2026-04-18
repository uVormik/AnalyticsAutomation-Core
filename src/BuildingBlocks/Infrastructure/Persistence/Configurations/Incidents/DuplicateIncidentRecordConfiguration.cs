using BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.Incidents;

public sealed class DuplicateIncidentRecordConfiguration : IEntityTypeConfiguration<DuplicateIncidentRecord>
{
    public void Configure(EntityTypeBuilder<DuplicateIncidentRecord> builder)
    {
        builder.ToTable("duplicate_incidents");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.DuplicateCandidateId).HasColumnName("duplicate_candidate_id");
        builder.Property(item => item.SourceVideoAssetId).HasColumnName("source_video_asset_id");
        builder.Property(item => item.MatchedVideoAssetId).HasColumnName("matched_video_asset_id");
        builder.Property(item => item.UploaderUserId).HasColumnName("uploader_user_id");
        builder.Property(item => item.UploaderGroupNodeId).HasColumnName("uploader_group_node_id");
        builder.Property(item => item.EscalatedHigher).HasColumnName("escalated_higher");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Severity)
            .HasColumnName("severity")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.MatchKind)
            .HasColumnName("match_kind")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(item => item.DuplicateCandidateId)
            .IsUnique()
            .HasDatabaseName("ux_duplicate_incidents_duplicate_candidate_id");

        builder.HasIndex(item => item.UploaderGroupNodeId)
            .HasDatabaseName("ix_duplicate_incidents_uploader_group_node_id");

        builder.HasIndex(item => item.Status)
            .HasDatabaseName("ix_duplicate_incidents_status");
    }
}