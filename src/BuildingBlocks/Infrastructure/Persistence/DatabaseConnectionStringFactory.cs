using Npgsql;

namespace BuildingBlocks.Infrastructure.Persistence;

public static class DatabaseConnectionStringFactory
{
    public static string Build(DatabaseOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return options.ConnectionString;
        }

        var passwordFilePath = ResolvePasswordFilePath(options.PasswordFilePath);
        if (!File.Exists(passwordFilePath))
        {
            throw new InvalidOperationException(
                $"Database password file was not found: {passwordFilePath}");
        }

        var password = File.ReadAllText(passwordFilePath).Trim();
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Database password file is empty: {passwordFilePath}");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = options.Host,
            Port = options.Port,
            Database = options.Database,
            Username = options.Username,
            Password = password,
            IncludeErrorDetail = true
        };

        return builder.ConnectionString;
    }

    public static string ResolvePasswordFilePath(string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AnalyticsAutomation-Core",
            "local-dev",
            "postgres-app-password.txt");
    }
}