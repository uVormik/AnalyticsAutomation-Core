using System.Globalization;
using BuildingBlocks.Contracts.VideoUpload;

namespace App.Web.Features.Upload.Models;

public static class UploadReceiptRequestFactory
{
    public static VideoUploadReceiptRequestDto Create(UploadReceiptFormModel form)
    {
        ArgumentNullException.ThrowIfNull(form);

        return new VideoUploadReceiptRequestDto(
            PreUploadCheckId: ParseGuid(form.PreUploadCheckId, nameof(form.PreUploadCheckId)),
            UserId: ParseGuid(form.UserId, nameof(form.UserId)),
            DeviceId: ParseOptionalGuid(form.DeviceId, nameof(form.DeviceId)),
            GroupNodeId: ParseOptionalGuid(form.GroupNodeId, nameof(form.GroupNodeId)),
            ExternalVideoId: Required(form.ExternalVideoId, nameof(form.ExternalVideoId)),
            StorageKey: Required(form.StorageKey, nameof(form.StorageKey)),
            SiteStatus: Required(form.SiteStatus, nameof(form.SiteStatus)),
            SizeBytes: PositiveLong(form.SizeBytes, nameof(form.SizeBytes)),
            ByteSha256: Required(form.ByteSha256, nameof(form.ByteSha256)),
            IdempotencyKey: Required(form.IdempotencyKey, nameof(form.IdempotencyKey)),
            UploadedAtUtc: ParseDateTimeOffset(form.UploadedAtUtc, nameof(form.UploadedAtUtc)));
    }

    private static Guid ParseGuid(string value, string fieldName)
    {
        if (Guid.TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName} must be a valid GUID.");
    }

    private static Guid? ParseOptionalGuid(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Guid.TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName} must be a valid GUID.");
    }

    private static string Required(string value, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException($"{fieldName} is required.");
    }

    private static string? OptionalTrimmed(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static long PositiveLong(long value, string fieldName)
    {
        if (value > 0)
        {
            return value;
        }

        throw new InvalidOperationException($"{fieldName} must be greater than zero.");
    }

    private static long? PositiveNullableLong(long value, string fieldName)
    {
        if (value == 0)
        {
            return null;
        }

        if (value > 0)
        {
            return value;
        }

        throw new InvalidOperationException($"{fieldName} must be greater than zero.");
    }

    private static DateTimeOffset ParseDateTimeOffset(string value, string fieldName)
    {
        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName} must be a valid UTC date/time.");
    }

    private static DateTimeOffset? ParseOptionalDateTimeOffset(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseDateTimeOffset(value, fieldName);
    }
}