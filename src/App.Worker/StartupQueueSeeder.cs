using App.Worker.Queues;

namespace App.Worker;

public sealed class StartupQueueSeeder(
    IBackgroundCommandQueue queue,
    ILogger<StartupQueueSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await queue.EnqueueAsync("bootstrap-noop", cancellationToken);

        logger.LogInformation(
            "Queued bootstrap background command for App.Worker.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Startup queue seeder stopped.");

        return Task.CompletedTask;
    }
}