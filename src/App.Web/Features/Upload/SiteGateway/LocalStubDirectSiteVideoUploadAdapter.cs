using System.Globalization;

using Microsoft.Extensions.Logging;

namespace App.Web.Features.Upload.SiteGateway;

public sealed class LocalStubDirectSiteVideoUploadAdapter : IDirectSiteVideoUploadAdapter
{
    public const string LocalStubSiteStatus = "uploaded";

    private static readonly Action<ILogger, Guid, Exception?> LogLocalStubCompleted =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(4001, nameof(LogLocalStubCompleted)),
            "Local direct-site upload stub completed for pre-upload check {PreUploadCheckId}. No video bytes were sent through App.Api.");

    private readonly ILogger<LocalStubDirectSiteVideoUploadAdapter> _logger;

    public LocalStubDirectSiteVideoUploadAdapter(
        ILogger<LocalStubDirectSiteVideoUploadAdapter> logger)
    {
        _logger = logger;
    }

    public Task<DirectSiteVideoUploadResult> UploadAsync(
        DirectSiteVideoUploadDraft draft,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        if (!content.CanRead)
        {
            return Task.FromResult(new DirectSiteVideoUploadResult(
                IsConfigured: true,
                Succeeded: false,
                ExternalVideoId: null,
                StorageKey: null,
                SiteStatus: null,
                Message: "Local direct-site upload stub requires a readable content stream."));
        }

        var suffix = draft.PreUploadCheckId.ToString("N");
        var safeFileName = SafeFileName(draft.FileName);

        LogLocalStubCompleted(_logger, draft.PreUploadCheckId, null);

        return Task.FromResult(new DirectSiteVideoUploadResult(
            IsConfigured: true,
            Succeeded: true,
            ExternalVideoId: string.Create(CultureInfo.InvariantCulture, $"local-stub-{suffix}"),
            StorageKey: string.Create(CultureInfo.InvariantCulture, $"local-stub/{suffix}/{safeFileName}"),
            SiteStatus: LocalStubSiteStatus,
            Message: "Local direct-site upload stub completed. No video bytes were sent through App.Api."));
    }

    private static string SafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "video.bin";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sanitizedChars = fileName
            .Trim()
            .Select(character => invalid.Contains(character) ? '-' : character)
            .ToArray();

        var sanitized = new string(sanitizedChars);

        return string.IsNullOrWhiteSpace(sanitized)
            ? "video.bin"
            : sanitized;
    }
}