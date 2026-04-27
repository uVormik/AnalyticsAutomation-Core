using App.Desktop.Boundaries;
using App.Desktop.Services.Auth;

namespace App.Desktop.Tests;

public sealed class DisabledDesktopSessionStoreTests
{
    [Fact]
    public async Task LoadSaveAndClearAreExplicitlyDisabledAndSafe()
    {
        var store = new DisabledDesktopSessionStore();
        var storedSession = CreateStoredSession();

        var loaded = await store.LoadAsync(CancellationToken.None);
        var saveResult = await store.SaveAsync(storedSession, CancellationToken.None);
        var clearResult = await store.ClearAsync(CancellationToken.None);

        Assert.Null(loaded);
        Assert.Equal(DesktopSessionStoreStatus.Disabled, saveResult.Status);
        Assert.Equal(DesktopSessionStoreStatus.Disabled, clearResult.Status);
        Assert.Contains("disabled", saveResult.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", saveResult.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(storedSession.AccessToken, saveResult.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(storedSession.RefreshToken!, saveResult.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledStoreDoesNotCreatePlaintextPersistenceFiles()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("aa-desktop-session-store-");

        try
        {
            var store = new DisabledDesktopSessionStore();

            _ = await store.SaveAsync(CreateStoredSession(), CancellationToken.None);
            _ = await store.ClearAsync(CancellationToken.None);

            Assert.Empty(Directory.EnumerateFileSystemEntries(
                tempDirectory.FullName,
                "*",
                SearchOption.AllDirectories));
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    private static DesktopStoredSession CreateStoredSession()
    {
        return DesktopStoredSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Desktop Operator",
            CreateSensitiveValue("access"),
            CreateSensitiveValue("refresh"),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(15),
            isOfflineRestricted: false);
    }

    private static string CreateSensitiveValue(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }
}