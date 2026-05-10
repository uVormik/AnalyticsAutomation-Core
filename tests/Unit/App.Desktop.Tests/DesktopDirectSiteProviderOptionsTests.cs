using App.Desktop.Services.Upload;

namespace App.Desktop.Tests;

public sealed class DesktopDirectSiteProviderOptionsTests
{
    [Fact]
    public void MissingProviderConfigKeepsProviderDisabled()
    {
        DesktopDirectSiteProviderOptions options = DesktopDirectSiteProviderOptions.FromEnvironmentValues(
            providerKey: null,
            providerBaseAddress: null);

        Assert.Same(DesktopDirectSiteProviderOptions.Disabled, options);
        Assert.False(options.IsProviderConfigPresent);
        Assert.False(options.IsProviderConfigValid);
        Assert.False(options.IsDirectSiteUploadAvailable);
        Assert.Null(options.ProviderKey);
        Assert.Null(options.ProviderBaseAddress);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a uri")]
    [InlineData("ftp://upload.korobochka.local")]
    [InlineData("http://upload.korobochka.local")]
    [InlineData("https://operator:secret@upload.korobochka.local")]
    [InlineData("https://upload.korobochka.local?preview=1")]
    [InlineData("https://upload.korobochka.local#secret")]
    [InlineData("https://upload.korobochka.local/video")]
    public void InvalidProviderBaseAddressKeepsProviderDisabled(string providerBaseAddress)
    {
        DesktopDirectSiteProviderOptions options = DesktopDirectSiteProviderOptions.FromEnvironmentValues(
            "korobochka-direct",
            providerBaseAddress);

        Assert.True(options.IsProviderConfigPresent);
        Assert.False(options.IsProviderConfigValid);
        Assert.False(options.IsDirectSiteUploadAvailable);
        Assert.Equal("korobochka-direct", options.ProviderKey);
        Assert.Null(options.ProviderBaseAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("provider key")]
    [InlineData("provider/key")]
    [InlineData("access-token")]
    [InlineData("secret-provider")]
    [InlineData("token-provider")]
    public void InvalidProviderKeyKeepsProviderDisabled(string? providerKey)
    {
        DesktopDirectSiteProviderOptions options = DesktopDirectSiteProviderOptions.FromEnvironmentValues(
            providerKey,
            "https://upload.korobochka.local");

        Assert.True(options.IsProviderConfigPresent);
        Assert.False(options.IsProviderConfigValid);
        Assert.False(options.IsDirectSiteUploadAvailable);
        Assert.Null(options.ProviderKey);
        Assert.NotNull(options.ProviderBaseAddress);
    }

    [Fact]
    public void ValidProviderConfigIsRecognizedAsConfigOnlyBoundary()
    {
        DesktopDirectSiteProviderOptions options = DesktopDirectSiteProviderOptions.FromEnvironmentValues(
            "korobochka-direct",
            "https://upload.korobochka.local");

        Assert.True(options.IsProviderConfigPresent);
        Assert.True(options.IsProviderConfigValid);
        Assert.False(options.IsDirectSiteUploadAvailable);
        Assert.Equal("korobochka-direct", options.ProviderKey);
        Assert.Equal(new Uri("https://upload.korobochka.local"), options.ProviderBaseAddress);
    }

    [Fact]
    public void ToStringKeepsProviderDiagnosticsRedactedAndSecretFree()
    {
        DesktopDirectSiteProviderOptions invalidOptions = DesktopDirectSiteProviderOptions.FromEnvironmentValues(
            "token-provider",
            "https://upload.korobochka.local?preview=1");
        DesktopDirectSiteProviderOptions validOptions = DesktopDirectSiteProviderOptions.FromEnvironmentValues(
            "korobochka-direct",
            "https://upload.korobochka.local");

        string invalidText = invalidOptions.ToString();
        string validText = validOptions.ToString();

        Assert.DoesNotContain("preview", invalidText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token-provider", invalidText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", validText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/", validText, StringComparison.Ordinal);
        Assert.Contains("ProviderKey = korobochka-direct", validText, StringComparison.Ordinal);
        Assert.Contains("Host = upload.korobochka.local", validText, StringComparison.Ordinal);
    }
}