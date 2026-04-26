namespace App.Desktop.Boundaries;

public interface IBlazorWebViewHostBoundary
{
    string SurfaceName { get; }

    string HostPage { get; }

    IReadOnlyList<BlazorRootComponentDescriptor> GetRootComponents();

    IReadOnlyList<SharedUiComponentDescriptor> GetSelectedSharedComponents();
}

public sealed record BlazorRootComponentDescriptor(
    string Selector,
    Type ComponentType);

public sealed record SharedUiComponentDescriptor(
    string Name,
    Type ComponentType);