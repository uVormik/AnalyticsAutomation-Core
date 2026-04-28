using App.Maintenance.Configuration;
using App.Maintenance.IdentityAdmin;
using App.Maintenance.IdentityBootstrap;
using App.Maintenance.IncidentRoutingAdmins;
using App.Maintenance.IntegrationAccounts;

using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

return await AppMaintenanceProgram.RunAsync(args);

internal static class AppMaintenanceProgram
{
    public static Task<int> RunAsync(string[] args)
    {
        return RunAsync(
            args,
            CreateHost,
            Console.Out,
            Console.Error,
            CancellationToken.None);
    }

    internal static async Task<int> RunAsync(
        string[] args,
        Func<IHost> hostFactory,
        TextWriter standardOutput,
        TextWriter errorOutput,
        CancellationToken cancellationToken)
    {
        if (!TryParseCommand(args, out var command))
        {
            WriteUsage(errorOutput);
            return 1;
        }

        try
        {
            using IHost host = hostFactory();
            using IServiceScope scope = host.Services.CreateScope();

            switch (command)
            {
                case MaintenanceCommand.IntegrationAccountUpsert:
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IntegrationAccountMaintenanceService>();
                        var result = await service.UpsertAsync(cancellationToken);

                        WriteIntegrationAccountResult(standardOutput, result);
                        return 0;
                    }

                case MaintenanceCommand.IncidentRoutingAdminUpsert:
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IncidentRoutingAdminMaintenanceService>();
                        var result = await service.UpsertAsync(cancellationToken);

                        WriteIncidentRoutingAdminResult(standardOutput, result);
                        return 0;
                    }

                case MaintenanceCommand.IdentityBootstrapFirstAdmin:
                    {
                        var service = scope.ServiceProvider.GetRequiredService<FirstAdminBootstrapMaintenanceService>();
                        var result = await service.BootstrapAsync(cancellationToken);

                        WriteFirstAdminBootstrapResult(standardOutput, result);
                        return 0;
                    }

                case MaintenanceCommand.IdentityAdminResetPassword:
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IdentityAdminPasswordRecoveryMaintenanceService>();
                        var result = await service.ResetPasswordAsync(cancellationToken);

                        WriteIdentityAdminPasswordRecoveryResult(standardOutput, result);
                        return 0;
                    }

                default:
                    throw new InvalidOperationException("Maintenance command is not supported.");
            }
        }
        catch (InvalidOperationException ex)
        {
            WriteSafeError(errorOutput, ex.Message);
            return 1;
        }
        catch
        {
            errorOutput.WriteLine("Maintenance command failed.");
            return 1;
        }
    }

    private static IHost CreateHost()
    {
        var configuration = AppApiConfigurationLoader.Load();
        var databaseOptions = DatabaseOptionsFactory.Create(configuration);

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddPlatformPersistence(databaseOptions);
        builder.Services.AddSingleton<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
        builder.Services.AddScoped<IncidentRoutingAdminMaintenanceService>();
        builder.Services.AddScoped<FirstAdminBootstrapMaintenanceService>();
        builder.Services.AddScoped<IdentityAdminPasswordRecoveryMaintenanceService>();
        builder.Services.AddScoped<IntegrationAccountMaintenanceService>();

        return builder.Build();
    }

    private static bool TryParseCommand(string[] args, out MaintenanceCommand command)
    {
        if (args.Length == 2
            && string.Equals(args[0], "integration-account", StringComparison.OrdinalIgnoreCase)
            && string.Equals(args[1], "upsert", StringComparison.OrdinalIgnoreCase))
        {
            command = MaintenanceCommand.IntegrationAccountUpsert;
            return true;
        }

        if (args.Length == 2
            && string.Equals(args[0], "incident-routing-admin", StringComparison.OrdinalIgnoreCase)
            && string.Equals(args[1], "upsert", StringComparison.OrdinalIgnoreCase))
        {
            command = MaintenanceCommand.IncidentRoutingAdminUpsert;
            return true;
        }

        if (args.Length == 2
            && string.Equals(args[0], "identity-bootstrap", StringComparison.OrdinalIgnoreCase)
            && string.Equals(args[1], "first-admin", StringComparison.OrdinalIgnoreCase))
        {
            command = MaintenanceCommand.IdentityBootstrapFirstAdmin;
            return true;
        }

        if (args.Length == 2
            && string.Equals(args[0], "identity-admin", StringComparison.OrdinalIgnoreCase)
            && string.Equals(args[1], "reset-password", StringComparison.OrdinalIgnoreCase))
        {
            command = MaintenanceCommand.IdentityAdminResetPassword;
            return true;
        }

        command = default;
        return false;
    }

    private static void WriteIntegrationAccountResult(
        TextWriter writer,
        IntegrationAccountUpsertResult result)
    {
        writer.WriteLine($"login: {result.Login}");
        writer.WriteLine($"status: {(result.WasCreated ? "created" : "updated")}");
        writer.WriteLine($"role: {result.AssignedRoleCode}");
        writer.WriteLine($"group-node: {result.AssignedGroupNodeCode}");
    }

    private static void WriteIncidentRoutingAdminResult(
        TextWriter writer,
        IncidentRoutingAdminUpsertResult result)
    {
        writer.WriteLine($"login: {result.Login}");
        writer.WriteLine($"status: {(result.WasCreated ? "created" : "updated")}");
        writer.WriteLine($"role: {result.AssignedRoleCode}");
        writer.WriteLine($"role-link: {(result.WasRoleLinkCreated ? "created" : "exists")}");
        writer.WriteLine($"group-node: {result.AssignedGroupNodeCode}");
        writer.WriteLine($"assignment: {(result.WasAssignmentCreated ? "created" : "exists")}");
    }

    private static void WriteFirstAdminBootstrapResult(
        TextWriter writer,
        FirstAdminBootstrapResult result)
    {
        writer.WriteLine($"login: {result.Login}");
        writer.WriteLine($"status: {FormatFirstAdminStatus(result.Status)}");
        writer.WriteLine($"role: {result.AssignedRoleCode}");
        writer.WriteLine($"role-link: {FormatCreatedOrExists(result.WasRoleLinkCreated, result.Status)}");
        writer.WriteLine($"group-node: {result.AssignedGroupNodeCode}");
        writer.WriteLine($"assignment: {FormatCreatedOrExists(result.WasAssignmentCreated, result.Status)}");
        writer.WriteLine($"audit: {result.AuditAction}");
    }

    private static string FormatFirstAdminStatus(FirstAdminBootstrapStatus status)
    {
        return status switch
        {
            FirstAdminBootstrapStatus.Created => "created",
            FirstAdminBootstrapStatus.Updated => "updated",
            FirstAdminBootstrapStatus.SkippedExistingAdmin => "skipped_existing_admin",
            _ => "unknown"
        };
    }

    private static string FormatCreatedOrExists(
        bool wasCreated,
        FirstAdminBootstrapStatus status)
    {
        if (status == FirstAdminBootstrapStatus.SkippedExistingAdmin)
        {
            return "skipped";
        }

        return wasCreated ? "created" : "exists";
    }

    private static void WriteIdentityAdminPasswordRecoveryResult(
        TextWriter writer,
        IdentityAdminPasswordRecoveryResult result)
    {
        writer.WriteLine($"login: {result.Login}");
        writer.WriteLine($"mode: reset-password");
        writer.WriteLine($"status: {FormatIdentityAdminPasswordRecoveryStatus(result.Status)}");
        writer.WriteLine($"role: {result.AssignedRoleCode}");
        writer.WriteLine($"group-node: {result.AssignedGroupNodeCode}");
        writer.WriteLine($"audit: {result.AuditAction}");
    }

    private static string FormatIdentityAdminPasswordRecoveryStatus(
        IdentityAdminPasswordRecoveryStatus status)
    {
        return status switch
        {
            IdentityAdminPasswordRecoveryStatus.PasswordReset => "password_reset",
            _ => "unknown"
        };
    }

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project tools\\App.Maintenance\\App.Maintenance.csproj -- integration-account upsert");
        writer.WriteLine("  dotnet run --project tools\\App.Maintenance\\App.Maintenance.csproj -- incident-routing-admin upsert");
        writer.WriteLine("  dotnet run --project tools\\App.Maintenance\\App.Maintenance.csproj -- identity-bootstrap first-admin");
        writer.WriteLine("  dotnet run --project tools\\App.Maintenance\\App.Maintenance.csproj -- identity-admin reset-password");
    }

    private static void WriteSafeError(TextWriter writer, string message)
    {
        writer.WriteLine(RedactSecretLikeValues(message));
    }

    private static string RedactSecretLikeValues(string message)
    {
        var redacted = RedactKeyValue(message, "Password");
        redacted = RedactKeyValue(redacted, "Pwd");
        redacted = RedactKeyValue(redacted, "AccessToken");
        redacted = RedactKeyValue(redacted, "RefreshToken");
        redacted = RedactAuthorizationHeader(redacted);

        return redacted;
    }

    private static string RedactKeyValue(string message, string key)
    {
        var marker = $"{key}=";
        var start = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return message;
        }

        var valueStart = start + marker.Length;
        var valueEnd = message.IndexOfAny([';', ' ', '\r', '\n'], valueStart);
        if (valueEnd < 0)
        {
            valueEnd = message.Length;
        }

        return message[..valueStart] + "<redacted>" + message[valueEnd..];
    }

    private static string RedactAuthorizationHeader(string message)
    {
        const string marker = "Authorization:";
        var start = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return message;
        }

        var valueStart = start + marker.Length;
        var valueEnd = message.IndexOfAny(['\r', '\n'], valueStart);
        if (valueEnd < 0)
        {
            valueEnd = message.Length;
        }

        return message[..valueStart] + " <redacted>" + message[valueEnd..];
    }

    private enum MaintenanceCommand
    {
        IntegrationAccountUpsert,
        IncidentRoutingAdminUpsert,
        IdentityBootstrapFirstAdmin,
        IdentityAdminResetPassword
    }
}