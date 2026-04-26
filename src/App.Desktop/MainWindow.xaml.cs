using System.Windows;

namespace App.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Resources.Add("services", services);
        InitializeComponent();
    }
}