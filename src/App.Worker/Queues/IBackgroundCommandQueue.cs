namespace App.Worker.Queues;

public interface IBackgroundCommandQueue
{
    ValueTask EnqueueAsync(string commandName, CancellationToken cancellationToken = default);
    ValueTask<string> DequeueAsync(CancellationToken cancellationToken);
}