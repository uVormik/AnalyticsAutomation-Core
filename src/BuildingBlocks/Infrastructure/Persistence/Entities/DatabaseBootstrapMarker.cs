namespace BuildingBlocks.Infrastructure.Persistence.Entities;

public sealed class DatabaseBootstrapMarker
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}