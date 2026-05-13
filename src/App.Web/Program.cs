using App.Web.Features.Upload.Api;
using App.Web.Features.Upload.Configuration;
using App.Web.Features.Upload.ControlPlane;
using App.Web.Features.Upload.Services;
using App.Web.Features.Upload.SiteGateway;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using App.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<global::App.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IVideoUploadApi, HttpVideoUploadApi>();
builder.Services.AddScoped<IUploadControlPlaneApi, HttpUploadControlPlaneApi>();
builder.Services.AddScoped<IUploadControlPlaneSessionStore, InMemoryUploadControlPlaneSessionStore>();
builder.Services.AddScoped<IDirectSiteVideoUploadAdapter, LocalStubDirectSiteVideoUploadAdapter>();
builder.Services.AddScoped<IUploadOnlineStatusProvider, BrowserUploadOnlineStatusProvider>();
builder.Services.AddScoped<IUploadSiteConnectionFeatureGate, ConfigurationUploadSiteConnectionFeatureGate>();

await builder.Build().RunAsync();