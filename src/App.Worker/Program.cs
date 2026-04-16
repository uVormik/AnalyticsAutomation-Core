using App.Worker.Queues;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
});

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});

builder.Services.AddSingleton<IBackgroundCommandQueue, InMemoryBackgroundCommandQueue>();
builder.Services.AddHostedService<global::App.Worker.StartupQueueSeeder>();
builder.Services.AddHostedService<global::App.Worker.Worker>();

var host = builder.Build();

host.Run();