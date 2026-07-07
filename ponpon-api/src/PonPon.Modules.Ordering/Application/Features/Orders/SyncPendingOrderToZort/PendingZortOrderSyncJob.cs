using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PonPon.Modules.Ordering.Application.Abstractions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.SyncPendingOrderToZort;

public sealed class PendingZortOrderSyncJob : BackgroundService
{
    private const int BatchSize = 20;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PendingZortOrderSyncJob> _logger;

    public PendingZortOrderSyncJob(
        IServiceProvider serviceProvider,
        ILogger<PendingZortOrderSyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        using var stoppingRegistration = stoppingToken.Register(timer.Dispose);

        await ProcessPendingOrdersAsync(stoppingToken);

        try
        {
            while (await timer.WaitForNextTickAsync())
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await ProcessPendingOrdersAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal BackgroundService shutdown.
        }
    }

    private async Task ProcessPendingOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var handler = scope.ServiceProvider.GetRequiredService<SyncPendingOrderToZortHandler>();

            var pendingOrders = await orders.GetPendingZortSyncAsync(BatchSize, cancellationToken);
            if (pendingOrders.Count == 0)
                return;

            _logger.LogInformation("Found {Count} local orders pending ZORT sync", pendingOrders.Count);

            foreach (var order in pendingOrders)
            {
                try
                {
                    await handler.HandleAsync(order, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync local order {OrderNumber} to ZORT", order.Number);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process local orders pending ZORT sync");
        }
    }
}
