using System.Collections.Generic;

using App.Worker.Queues;

namespace App.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IBackgroundCommandQueue queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("App.Worker execution loop started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var commandName = await queue.DequeueAsync(stoppingToken);

                using var _ = logger.BeginScope(new Dictionary<string, object?>
                {
                    ["CommandName"] = commandName
                });

                logger.LogInformation("Dequeued background command.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                logger.LogInformation("Background command processed.");
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("App.Worker stopping by cancellation.");
        }
    }
}