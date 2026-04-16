using System.Reflection;

namespace BuildingBlocks.Infrastructure.AssemblyMetadata;

public static class ApplicationVersionReader
{
    public static string GetVersion<TMarker>()
    {
        var assembly = typeof(TMarker).Assembly;

        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}