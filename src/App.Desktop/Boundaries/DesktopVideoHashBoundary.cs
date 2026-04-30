namespace App.Desktop.Boundaries;

public interface IDesktopVideoHashService
{
    ValueTask<DesktopVideoHashResult> CalculateSha256Async(
        DesktopVideoHashRequest request,
        CancellationToken cancellationToken);
}

public sealed class DesktopVideoHashRequest
{
    private DesktopVideoHashRequest(DesktopVideoHashSource source)
    {
        Source = source;
    }

    internal DesktopVideoHashSource Source { get; }

    internal static DesktopVideoHashRequest FromSource(DesktopVideoHashSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new DesktopVideoHashRequest(source);
    }

    internal bool TryGetLocalFilePath(out string filePath)
    {
        return Source.TryGetLocalFilePath(out filePath);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopVideoHashRequest)} {{ Source = {Source.Kind} }}";
    }
}

public sealed class DesktopVideoHashSource
{
    private DesktopVideoHashSource(DesktopVideoHashSourceKind kind, string? localFilePath)
    {
        Kind = kind;
        LocalFilePath = localFilePath;
    }

    internal DesktopVideoHashSourceKind Kind { get; }

    private string? LocalFilePath { get; }

    internal static DesktopVideoHashSource VisualSmoke { get; } = new(
        DesktopVideoHashSourceKind.VisualSmoke,
        localFilePath: null);

    internal static DesktopVideoHashSource FromLocalFilePath(string localFilePath)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            return new DesktopVideoHashSource(
                DesktopVideoHashSourceKind.Unavailable,
                localFilePath: null);
        }

        return new DesktopVideoHashSource(
            DesktopVideoHashSourceKind.LocalFile,
            localFilePath);
    }

    internal bool TryGetLocalFilePath(out string filePath)
    {
        filePath = string.Empty;

        if (Kind != DesktopVideoHashSourceKind.LocalFile || string.IsNullOrWhiteSpace(LocalFilePath))
        {
            return false;
        }

        filePath = LocalFilePath;
        return true;
    }

    public override string ToString()
    {
        return $"{nameof(DesktopVideoHashSource)} {{ Kind = {Kind} }}";
    }
}

internal enum DesktopVideoHashSourceKind
{
    Unavailable,
    LocalFile,
    VisualSmoke
}

public sealed class DesktopVideoHashResult
{
    private DesktopVideoHashResult(
        DesktopVideoHashStatus status,
        string? sha256Hex)
    {
        Status = status;
        Sha256Hex = sha256Hex;
    }

    public DesktopVideoHashStatus Status { get; }

    public string? Sha256Hex { get; }

    public static DesktopVideoHashResult Canceled { get; } = new(
        DesktopVideoHashStatus.Canceled,
        sha256Hex: null);

    public static DesktopVideoHashResult Unavailable { get; } = new(
        DesktopVideoHashStatus.Unavailable,
        sha256Hex: null);

    public static DesktopVideoHashResult Failed { get; } = new(
        DesktopVideoHashStatus.Failed,
        sha256Hex: null);

    public static DesktopVideoHashResult Succeeded(string sha256Hex)
    {
        if (!IsLowercaseSha256Hex(sha256Hex))
        {
            throw new ArgumentException("SHA-256 must be a 64-character lowercase hex value.", nameof(sha256Hex));
        }

        return new DesktopVideoHashResult(
            DesktopVideoHashStatus.Succeeded,
            sha256Hex);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopVideoHashResult)} {{ Status = {Status}, HasSha256 = {Sha256Hex is not null} }}";
    }

    private static bool IsLowercaseSha256Hex(string? value)
    {
        if (value is null || value.Length != 64)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool isDigit = character is >= '0' and <= '9';
            bool isLowerHex = character is >= 'a' and <= 'f';
            if (!isDigit && !isLowerHex)
            {
                return false;
            }
        }

        return true;
    }
}

public enum DesktopVideoHashStatus
{
    Succeeded,
    Canceled,
    Unavailable,
    Failed
}