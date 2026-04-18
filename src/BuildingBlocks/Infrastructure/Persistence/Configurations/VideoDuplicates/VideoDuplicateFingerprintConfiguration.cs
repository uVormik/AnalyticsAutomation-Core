using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.VideoDuplicates;

public sealed class VideoDuplicateFingerprintConfiguration : IEntityTypeConfiguration<VideoDuplicateFingerprint>
{
    public void Configure(EntityTypeBuilder<VideoDuplicateFingerprint> builder)
    {
        builder.ToTable("video_duplicate_fingerprints");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.VideoAssetId).HasColumnName("video_asset_id");

        builder.Property(item => item.FingerprintKind)
            .HasColumnName("fingerprint_kind")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.FingerprintValue)
            .HasColumnName("fingerprint_value")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.SizeBytes).HasColumnName("size_bytes");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.HasIndex(item => new { item.FingerprintKind, item.FingerprintValue, item.SizeBytes })
            .HasDatabaseName("ix_video_duplicate_fingerprints_kind_value_size");

        builder.HasIndex(item => item.VideoAssetId)
            .HasDatabaseName("ix_video_duplicate_fingerprints_video_asset_id");
    }
}