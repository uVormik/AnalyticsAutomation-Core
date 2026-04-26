namespace App.Desktop.Boundaries;

public interface IControlPlaneApiClient
{
    ValueTask<ControlPlanePreUploadCheckResult> RequestPreUploadCheckAsync(
        ControlPlanePreUploadCheckRequest request,
        CancellationToken cancellationToken);

    ValueTask<ControlPlaneReceiptResult> RecordUploadReceiptAsync(
        ControlPlaneUploadReceiptDraft receipt,
        CancellationToken cancellationToken);
}

public sealed record ControlPlanePreUploadCheckRequest(
    string BusinessObjectKey,
    LocalFileMetadata FileMetadata);

public sealed record ControlPlanePreUploadCheckResult(
    ControlPlaneBoundaryStatus Status,
    string Reason);

public sealed record ControlPlaneUploadReceiptDraft(
    string BusinessObjectKey,
    string DirectSiteUploadReference);

public sealed record ControlPlaneReceiptResult(
    ControlPlaneBoundaryStatus Status,
    string Reason);

public enum ControlPlaneBoundaryStatus
{
    Deferred
}