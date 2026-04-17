namespace BuildingBlocks.Infrastructure.Persistence;

public sealed record DatabaseOptions
{
    public const string DefaultHost = "localhost";
    public const int DefaultPort = 5432;
    public const string DefaultDatabase = "analytics_automation_dev";
    public const string DefaultUsername = "analytics_automation_app_dev";

    public string? ConnectionString { get; init; }
    public string Host { get; init; } = DefaultHost;
    public int Port { get; init; } = DefaultPort;
    public string Database { get; init; } = DefaultDatabase;
    public string Username { get; init; } = DefaultUsername;
    public string? PasswordFilePath { get; init; }
}