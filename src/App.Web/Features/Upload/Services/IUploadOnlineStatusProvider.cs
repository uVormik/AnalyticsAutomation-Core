using Microsoft.JSInterop;

namespace App.Web.Features.Upload.Services;

public interface IUploadOnlineStatusProvider
{
    Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default);
}

public sealed class BrowserUploadOnlineStatusProvider : IUploadOnlineStatusProvider
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserUploadOnlineStatusProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>(
                "analyticsAutomationUpload.isOnline",
                cancellationToken);
        }
        catch (JSException)
        {
            return false;
        }
    }
}