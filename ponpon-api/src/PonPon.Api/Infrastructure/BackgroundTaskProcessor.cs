using Microsoft.Extensions.Hosting;

namespace PonPon.Api.Infrastructure;

public sealed class BackgroundTaskProcessor : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(45)];

    private readonly BackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTaskProcessor> _logger;

    public BackgroundTaskProcessor(
        BackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundTaskProcessor> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var work in _queue.ReadAllAsync(stoppingToken))
            {
                await ExecuteWithRetryAsync(work, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Background task processor stopped.");
        }
    }

    private async Task ExecuteWithRetryAsync(
        Func<IServiceProvider, CancellationToken, Task> work,
        CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= RetryDelays.Length + 1; attempt++)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await work(scope.ServiceProvider, stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt <= RetryDelays.Length)
            {
                var delay = RetryDelays[attempt - 1];
                _logger.LogWarning(ex,
                    "Background task failed (attempt {Attempt}/{MaxAttempts}), retrying in {Delay}s",
                    attempt, RetryDelays.Length + 1, delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Background task failed after {MaxAttempts} attempts, giving up",
                    RetryDelays.Length + 1);
            }
        }
    }
}
