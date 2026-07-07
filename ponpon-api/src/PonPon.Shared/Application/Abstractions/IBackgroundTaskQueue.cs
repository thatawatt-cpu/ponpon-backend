namespace PonPon.Shared.Application.Abstractions;

public interface IBackgroundTaskQueue
{
    void Enqueue(Func<IServiceProvider, CancellationToken, Task> work);
}
