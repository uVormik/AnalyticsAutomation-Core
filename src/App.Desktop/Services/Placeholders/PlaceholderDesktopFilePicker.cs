using App.Desktop.Boundaries;

namespace App.Desktop.Services.Placeholders;

public sealed class PlaceholderDesktopFilePicker : IDesktopFilePicker
{
    public ValueTask<IReadOnlyList<DesktopPickedFile>> PickVideoFilesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<DesktopPickedFile> noSelection = [];
        return ValueTask.FromResult(noSelection);
    }
}