using System.Threading.Channels;

namespace App.Worker.Queues;

public sealed class InMemoryBackgroundCommandQueue : IBackgroundCommandQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

    public ValueTask EnqueueAsync(
        string commandName,
        CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(commandName, cancellationToken);
    }

    public ValueTask<string> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}