using System.Threading.Channels;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Api.Infrastructure;

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>(
            new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Func<IServiceProvider, CancellationToken, Task> work)
        => _channel.Writer.TryWrite(work);

    public IAsyncEnumerable<Func<IServiceProvider, CancellationToken, Task>> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
