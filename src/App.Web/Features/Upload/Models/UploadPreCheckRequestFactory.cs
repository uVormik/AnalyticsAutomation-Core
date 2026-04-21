using System.Globalization;
using BuildingBlocks.Contracts.VideoUpload;

namespace App.Web.Features.Upload.Models;

public static class UploadPreCheckRequestFactory
{
    public static VideoPreUploadCheckRequestDto Create(UploadPreCheckFormModel form)
    {
        ArgumentNullException.ThrowIfNull(form);

        return new VideoPreUploadCheckRequestDto(
            UserId: ParseGuid(form.UserId, nameof(form.UserId)),
            DeviceId: ParseOptionalGuid(form.DeviceId, nameof(form.DeviceId)),
            GroupNodeId: ParseOptionalGuid(form.GroupNodeId, nameof(form.GroupNodeId)),
            BusinessObjectKey: Required(form.BusinessObjectKey, nameof(form.BusinessObjectKey)),
            FileName: Required(form.FileName, nameof(form.FileName)),
            SizeBytes: PositiveLong(form.SizeBytes, nameof(form.SizeBytes)),
            ByteSha256: Required(form.ByteSha256, nameof(form.ByteSha256)),
            ContentType: OptionalTrimmed(form.ContentType),
            CapturedAtUtc: ParseDateTimeOffset(form.CapturedAtUtc, nameof(form.CapturedAtUtc)));
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
}