namespace App.Mobile.Android.Services.Local;

internal sealed class FileMobileReportDraftSnapshotStore :
    global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftSnapshotStore
{
    private static readonly global::System.Text.Json.JsonSerializerOptions SerializerOptions = new();
    private readonly string _storageDirectory;

    public FileMobileReportDraftSnapshotStore(string storageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);

        _storageDirectory = storageDirectory;
        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<IReadOnlyList<global::App.Mobile.Android.Reports.MobileReportDraft>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_storageDirectory);

        var snapshotPath = GetSnapshotPath();
        if (!File.Exists(snapshotPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(snapshotPath);
        var document = await global::System.Text.Json.JsonSerializer.DeserializeAsync<SnapshotDocument>(
            stream,
            SerializerOptions,
            cancellationToken);

        return document?.Drafts?
            .Select(snapshot => snapshot.ToDraft())
            .ToArray()
            ?? [];
    }

    public async Task SaveAsync(
        IReadOnlyList<global::App.Mobile.Android.Reports.MobileReportDraft> drafts,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(drafts);
        Directory.CreateDirectory(_storageDirectory);

        var snapshotPath = GetSnapshotPath();
        if (drafts.Count == 0)
        {
            if (File.Exists(snapshotPath))
            {
                File.Delete(snapshotPath);
            }

            return;
        }

        var document = new SnapshotDocument(
            drafts.Select(ReportDraftSnapshot.FromDraft).ToArray());

        await using var stream = File.Create(snapshotPath);
        await global::System.Text.Json.JsonSerializer.SerializeAsync(
            stream,
            document,
            SerializerOptions,
            cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private string GetSnapshotPath()
    {
        return Path.Combine(_storageDirectory, "mobile-report-drafts-snapshot.json");
    }

    private sealed record SnapshotDocument(ReportDraftSnapshot[] Drafts);

    private sealed record ReportDraftSnapshot(
        string DraftId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        string Title,
        global::App.Mobile.Android.Reports.MobileReportDraftStatus Status,
        global::App.Mobile.Android.Reports.MobileReportDraftFieldValue[] Fields,
        global::App.Mobile.Android.Reports.MobileReportAttachment[] Attachments,
        bool IsRestoredFromSnapshot)
    {
        public global::App.Mobile.Android.Reports.MobileReportDraft ToDraft()
        {
            return new global::App.Mobile.Android.Reports.MobileReportDraft(
                DraftId,
                CreatedAtUtc,
                UpdatedAtUtc,
                Title,
                Status,
                Fields ?? [],
                Attachments ?? [])
            {
                IsRestoredFromSnapshot = IsRestoredFromSnapshot
            };
        }

        public static ReportDraftSnapshot FromDraft(
            global::App.Mobile.Android.Reports.MobileReportDraft draft)
        {
            return new ReportDraftSnapshot(
                draft.DraftId,
                draft.CreatedAtUtc,
                draft.UpdatedAtUtc,
                draft.Title,
                draft.Status,
                draft.Fields.ToArray(),
                draft.Attachments.ToArray(),
                draft.IsRestoredFromSnapshot);
        }
    }
}