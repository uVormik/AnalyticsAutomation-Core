using App.Web.Features.Upload.Configuration;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadSiteConnectionFeatureGateTests
{
    [Fact]
    public void IsEnabledDefaultsToFalseWhenFlagIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var gate = new ConfigurationUploadSiteConnectionFeatureGate(configuration);

        Assert.False(gate.IsEnabled);
    }

    [Fact]
    public void IsEnabledReadsWebUploadFlag()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConfigurationUploadSiteConnectionFeatureGate.ConfigurationKey] = "true"
            })
            .Build();

        var gate = new ConfigurationUploadSiteConnectionFeatureGate(configuration);

        Assert.True(gate.IsEnabled);
        Assert.Equal(
            "App.Web.Upload.SiteConnectionBaselineEnabled",
            ConfigurationUploadSiteConnectionFeatureGate.FlagName);
    }

    [Fact]
    public void IsEnabledTreatsInvalidValueAsOff()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConfigurationUploadSiteConnectionFeatureGate.ConfigurationKey] = "not-bool"
            })
            .Build();

        var gate = new ConfigurationUploadSiteConnectionFeatureGate(configuration);

        Assert.False(gate.IsEnabled);
    }
}