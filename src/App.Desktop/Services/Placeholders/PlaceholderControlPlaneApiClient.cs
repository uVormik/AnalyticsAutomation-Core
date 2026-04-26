using App.Desktop.Boundaries;

namespace App.Desktop.Services.Placeholders;

public sealed class PlaceholderControlPlaneApiClient : IControlPlaneApiClient
{
    private const string DeferredReason = "S2-48 skeleton only; no App.Api control-plane call is made.";

    public ValueTask<ControlPlanePreUploadCheckResult> RequestPreUploadCheckAsync(
        ControlPlanePreUploadCheckRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new ControlPlanePreUploadCheckResult(
            ControlPlaneBoundaryStatus.Deferred,
            DeferredReason));
    }

    public ValueTask<ControlPlaneReceiptResult> RecordUploadReceiptAsync(
        ControlPlaneUploadReceiptDraft receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new ControlPlaneReceiptResult(
            ControlPlaneBoundaryStatus.Deferred,
            DeferredReason));
    }
}