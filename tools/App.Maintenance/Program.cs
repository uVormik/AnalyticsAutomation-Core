using App.Maintenance.Configuration;
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

                default:
                    throw new InvalidOperationException("Maintenance command is not supported.");
            }
        }
        catch (InvalidOperationException ex)
        {
            errorOutput.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            errorOutput.WriteLine($"Maintenance command failed: {ex.Message}");
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

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project tools\\App.Maintenance\\App.Maintenance.csproj -- integration-account upsert");
        writer.WriteLine("  dotnet run --project tools\\App.Maintenance\\App.Maintenance.csproj -- incident-routing-admin upsert");
        writer.WriteLine("  dotnet run --project tools\\App.Maintenance\\App.Maintenance.csproj -- identity-bootstrap first-admin");
    }

    private enum MaintenanceCommand
    {
        IntegrationAccountUpsert,
        IncidentRoutingAdminUpsert,
        IdentityBootstrapFirstAdmin
    }
}