using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;

namespace App.Mobile.Android;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        BlazorHost.RootComponents.Clear();
        BlazorHost.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(global::App.Mobile.Android.Components.Routes)
        });
    }
}