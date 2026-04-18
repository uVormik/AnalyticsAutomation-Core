using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoDuplicates;

public sealed class VideoDuplicateCandidateConfiguration : IEntityTypeConfiguration<VideoDuplicateCandidate>
{
    public void Configure(EntityTypeBuilder<VideoDuplicateCandidate> builder)
    {
        builder.ToTable("video_duplicate_candidates");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.SourceVideoAssetId).HasColumnName("source_video_asset_id");
        builder.Property(item => item.MatchedVideoAssetId).HasColumnName("matched_video_asset_id");

        builder.Property(item => item.MatchKind)
            .HasColumnName("match_kind")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.Decision)
            .HasColumnName("decision")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.DetectedAtUtc).HasColumnName("detected_at_utc");

        builder.HasIndex(item => item.SourceVideoAssetId)
            .HasDatabaseName("ix_video_duplicate_candidates_source_asset_id");

        builder.HasIndex(item => item.MatchedVideoAssetId)
            .HasDatabaseName("ix_video_duplicate_candidates_matched_asset_id");

        builder.HasIndex(item => new { item.SourceVideoAssetId, item.MatchedVideoAssetId, item.MatchKind })
            .IsUnique()
            .HasDatabaseName("ux_video_duplicate_candidates_pair_kind");
    }
}