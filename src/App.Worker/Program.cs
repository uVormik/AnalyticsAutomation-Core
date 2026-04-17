using App.Worker.Queues;

using BuildingBlocks.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
});

var databaseOptions = new DatabaseOptions
{
    ConnectionString = builder.Configuration.GetConnectionString("MainDatabase"),
    Host = builder.Configuration["Database:Host"] ?? DatabaseOptions.DefaultHost,
    Port = int.TryParse(builder.Configuration["Database:Port"], out var databasePort)
        ? databasePort
        : DatabaseOptions.DefaultPort,
    Database = builder.Configuration["Database:Database"] ?? DatabaseOptions.DefaultDatabase,
    Username = builder.Configuration["Database:Username"] ?? DatabaseOptions.DefaultUsername,
    PasswordFilePath = builder.Configuration["Database:PasswordFilePath"]
};

builder.Services.AddPlatformPersistence(databaseOptions);

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});

builder.Services.AddSingleton<IBackgroundCommandQueue, InMemoryBackgroundCommandQueue>();
builder.Services.AddHostedService<global::App.Worker.StartupQueueSeeder>();
builder.Services.AddHostedService<global::App.Worker.Worker>();

var host = builder.Build();

host.Run();