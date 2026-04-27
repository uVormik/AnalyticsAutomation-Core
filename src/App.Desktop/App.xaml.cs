using System.Windows;

using App.Desktop.Boundaries;
using App.Desktop.Composition;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop;

public partial class DesktopApplication : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = DesktopCompositionRoot.BuildServicesFromEnvironment();
        _services
            .GetRequiredService<IDesktopShellLifecycle>()
            .OnStartingAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        MainWindow = new MainWindow(_services);
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_services is not null)
        {
            _services
                .GetRequiredService<IDesktopShellLifecycle>()
                .OnStoppingAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            _services.Dispose();
        }

        base.OnExit(e);
    }
}