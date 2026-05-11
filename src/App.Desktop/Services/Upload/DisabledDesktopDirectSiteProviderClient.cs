namespace App.Desktop.Services.Upload;

public sealed class DisabledDesktopDirectSiteProviderClient : IDesktopDirectSiteProviderClient
{
    private const string DisabledFailureMessage = "Direct-site provider client is disabled.";

    public bool EnablesRealUpload { get; }

    public string DiagnosticText => ToString();

    public ValueTask<DesktopDirectSiteUploadResponse> UploadAsync(
        DesktopDirectSiteUploadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!request.IsValid || request.ProviderKey is null || request.CorrelationId is null)
        {
            return ValueTask.FromResult(DesktopDirectSiteUploadResponse.Invalid);
        }

        return ValueTask.FromResult(DesktopDirectSiteUploadResponse.FromFailure(
            request.ProviderKey,
            DesktopDirectSiteUploadResponseStatus.FailedTerminal,
            retryable: false,
            DesktopDirectSiteUploadFailureKind.ProviderUnavailable,
            DisabledFailureMessage,
            request.CorrelationId.Value.ToString("D")));
    }

    public override string ToString()
    {
        return $"{nameof(DisabledDesktopDirectSiteProviderClient)} {{ "
            + $"EnablesRealUpload = {EnablesRealUpload}, "
            + "Status = Disabled, Diagnostics = <redacted> }}";
    }
}