using BuildingBlocks.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations;

public sealed class DatabaseBootstrapMarkerConfiguration : IEntityTypeConfiguration<DatabaseBootstrapMarker>
{
    private static readonly Guid SeedId = Guid.Parse("1D53B52B-AB0F-4B7F-BAC7-1B7F3E5E7707");
    private static readonly DateTimeOffset SeedCreatedAtUtc = new(2026, 4, 16, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<DatabaseBootstrapMarker> builder)
    {
        builder.ToTable("database_bootstrap_markers");

        builder.HasKey(marker => marker.Id)
            .HasName("pk_database_bootstrap_markers");

        builder.Property(marker => marker.Id)
            .HasColumnName("id");

        builder.Property(marker => marker.Code)
            .HasColumnName("code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(marker => marker.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(marker => marker.Code)
            .HasDatabaseName("ux_database_bootstrap_markers_code")
            .IsUnique();

        builder.HasData(new DatabaseBootstrapMarker
        {
            Id = SeedId,
            Code = "initial_postgresql_foundation",
            CreatedAtUtc = SeedCreatedAtUtc
        });
    }
}