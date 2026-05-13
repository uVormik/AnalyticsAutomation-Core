using Microsoft.Extensions.Configuration;

namespace App.Web.Features.Upload.Configuration;

public interface IUploadSiteConnectionFeatureGate
{
    bool IsEnabled { get; }
}

public sealed class ConfigurationUploadSiteConnectionFeatureGate : IUploadSiteConnectionFeatureGate
{
    public const string FlagName = "App.Web.Upload.SiteConnectionBaselineEnabled";
    public const string ConfigurationKey = "FeatureFlags:App:Web:Upload:SiteConnectionBaselineEnabled";

    private readonly IConfiguration _configuration;

    public ConfigurationUploadSiteConnectionFeatureGate(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsEnabled
    {
        get
        {
            string? rawValue = _configuration[ConfigurationKey];

            return bool.TryParse(rawValue, out bool enabled) && enabled;
        }
    }
}