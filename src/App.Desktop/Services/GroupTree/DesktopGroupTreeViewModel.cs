using App.Desktop.Boundaries;

namespace App.Desktop.Services.GroupTree;

public sealed class DesktopGroupTreeViewModel(
    IDesktopGroupTreeClient groupTreeClient,
    DesktopGroupTreeOptions groupTreeOptions,
    DesktopGroupSelectionState groupSelectionState)
{
    private readonly IDesktopGroupTreeClient _groupTreeClient =
        groupTreeClient ?? throw new ArgumentNullException(nameof(groupTreeClient));
    private readonly DesktopGroupTreeOptions _groupTreeOptions =
        groupTreeOptions ?? throw new ArgumentNullException(nameof(groupTreeOptions));
    private readonly DesktopGroupSelectionState _groupSelectionState =
        groupSelectionState ?? throw new ArgumentNullException(nameof(groupSelectionState));

    public bool IsLoading { get; private set; }

    public bool IsDevFakeGroupTreeEnabled => _groupTreeOptions.IsDevFakeGroupTreeEnabled;

    public IReadOnlyList<DesktopGroupTreeNode> Nodes { get; private set; } = [];

    public bool HasNodes => Nodes.Count > 0;

    public DesktopSelectedGroupContext? SelectedGroup => _groupSelectionState.SelectedGroup;

    public bool HasSelectedGroup => SelectedGroup is not null;

    public string? SelectedGroupMessage => SelectedGroup?.SelectedGroupMessage;

    public string StatusMessage { get; private set; } = DesktopGroupTreeText.Description;

    public async ValueTask<DesktopGroupTreeLoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsDevFakeGroupTreeEnabled)
        {
            Nodes = [];
            StatusMessage = DesktopGroupTreeText.DisabledMessage;
            return DesktopGroupTreeLoadResult.Disabled;
        }

        IsLoading = true;
        StatusMessage = DesktopGroupTreeText.LoadingMessage;

        try
        {
            DesktopGroupTreeLoadResult result = await _groupTreeClient.GetGroupTreeAsync(cancellationToken);

            Nodes = result.Status == DesktopGroupTreeLoadStatus.Loaded
                ? result.Nodes
                : [];
            StatusMessage = result.Message;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Nodes = [];
            StatusMessage = DesktopGroupTreeText.SelectionUnavailableMessage;
            return DesktopGroupTreeLoadResult.Canceled;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public DesktopSelectedGroupContext? SelectGroup(string groupId)
    {
        if (!IsDevFakeGroupTreeEnabled)
        {
            StatusMessage = DesktopGroupTreeText.DisabledMessage;
            return null;
        }

        DesktopGroupTreeNode? node = Nodes.FirstOrDefault(
            candidate => string.Equals(candidate.Id, groupId, StringComparison.Ordinal));

        if (!_groupSelectionState.TrySelect(node))
        {
            StatusMessage = DesktopGroupTreeText.SelectionUnavailableMessage;
            return null;
        }

        StatusMessage = DesktopGroupTreeText.LoadedMessage;
        return _groupSelectionState.SelectedGroup;
    }

    public void ResetForBackOrSignOut()
    {
        IsLoading = false;
        Nodes = [];
        StatusMessage = DesktopGroupTreeText.Description;
        _groupSelectionState.Clear();
    }
}