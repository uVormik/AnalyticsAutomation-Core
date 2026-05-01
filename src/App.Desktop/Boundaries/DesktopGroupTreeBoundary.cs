namespace App.Desktop.Boundaries;

public interface IDesktopGroupTreeClient
{
    ValueTask<DesktopGroupTreeLoadResult> GetGroupTreeAsync(CancellationToken cancellationToken);
}

public static class DesktopGroupTreeText
{
    public const string Title = "Группы";
    public const string Description =
        "Раздел подготовлен. Реальная загрузка дерева групп из App.Api будет добавлена отдельным approved slice.";
    public const string NavigationCardMessage = "Открыть безопасный preview выбора группы.";
    public const string DisabledMessage =
        "Дерево групп пока доступно только в dev-smoke режиме. Реальный вызов App.Api будет добавлен отдельным approved slice.";
    public const string LoadingMessage = "Загружается desktop-local dev-smoke дерево групп.";
    public const string LoadedMessage = "Dev-smoke дерево групп загружено из desktop-local fake boundary.";
    public const string SelectionUnavailableMessage = "Выберите доступный dev-smoke узел группы.";
    public const string BackToWorkspaceButton = "Назад к рабочей области";
    public const string SelectGroupButton = "Выбрать";
    public const string ContinueToUploadButton = "Перейти к загрузке видео";
    public const string GroupIdLabel = "safe key/id preview";

    public static string CreateSelectedGroupMessage(string displayName)
    {
        return $"Выбрана группа: {displayName}";
    }
}

public sealed record DesktopGroupTreeNode(
    string Id,
    string DisplayName,
    int Depth,
    bool CanSelect)
{
    public const string RootId = "root";
    public const string RootDisplayName = "Вся организация";
    public const string BranchId = "branch-001";
    public const string BranchDisplayName = "Тестовая ветка";
    public const string SiteId = "site-001";
    public const string SiteDisplayName = "Тестовый объект";

    public static IReadOnlyList<DesktopGroupTreeNode> VisualSmokeNodes { get; } =
    [
        new(RootId, RootDisplayName, Depth: 0, CanSelect: false),
        new(BranchId, BranchDisplayName, Depth: 1, CanSelect: false),
        new(SiteId, SiteDisplayName, Depth: 2, CanSelect: true)
    ];

    public string Preview => $"{Id} / {DisplayName}";
}

public sealed record DesktopSelectedGroupContext(
    string Id,
    string DisplayName)
{
    public string SelectedGroupMessage => DesktopGroupTreeText.CreateSelectedGroupMessage(DisplayName);

    public override string ToString()
    {
        return $"{nameof(DesktopSelectedGroupContext)} {{ Id = {Id}, DisplayName = {DisplayName} }}";
    }
}

public sealed class DesktopGroupTreeLoadResult
{
    private DesktopGroupTreeLoadResult(
        DesktopGroupTreeLoadStatus status,
        IReadOnlyList<DesktopGroupTreeNode> nodes,
        string message)
    {
        Status = status;
        Nodes = nodes;
        Message = message;
    }

    public DesktopGroupTreeLoadStatus Status { get; }

    public IReadOnlyList<DesktopGroupTreeNode> Nodes { get; }

    public string Message { get; }

    public static DesktopGroupTreeLoadResult Disabled { get; } = new(
        DesktopGroupTreeLoadStatus.Disabled,
        [],
        DesktopGroupTreeText.DisabledMessage);

    public static DesktopGroupTreeLoadResult Loaded(IReadOnlyList<DesktopGroupTreeNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        return new DesktopGroupTreeLoadResult(
            DesktopGroupTreeLoadStatus.Loaded,
            nodes,
            DesktopGroupTreeText.LoadedMessage);
    }

    public static DesktopGroupTreeLoadResult Canceled { get; } = new(
        DesktopGroupTreeLoadStatus.Canceled,
        [],
        DesktopGroupTreeText.SelectionUnavailableMessage);

    public override string ToString()
    {
        return $"{nameof(DesktopGroupTreeLoadResult)} {{ Status = {Status}, NodeCount = {Nodes.Count}, "
            + $"Message = {Message} }}";
    }
}

public enum DesktopGroupTreeLoadStatus
{
    Disabled,
    Loaded,
    Canceled
}