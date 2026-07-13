using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.ConfirmReceived;

public sealed class AutoConfirmReceivedJob : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan AutoReceiveDelay = TimeSpan.FromDays(7);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoConfirmReceivedJob> _logger;

    public AutoConfirmReceivedJob(
        IServiceProvider serviceProvider,
        ILogger<AutoConfirmReceivedJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        using var stoppingRegistration = stoppingToken.Register(timer.Dispose);

        try
        {
            while (await timer.WaitForNextTickAsync())
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await ProcessAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal BackgroundService shutdown.
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();
            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

            var now = clock.UtcNow;
            var deliveredOrders = await orders.GetDeliveredUnreceivedOlderThanAsync(
                now.Subtract(AutoReceiveDelay),
                BatchSize,
                cancellationToken);

            if (deliveredOrders.Count == 0)
                return;

            foreach (var order in deliveredOrders)
            {
                order.MarkReceived(now);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Auto-confirmed {Count} delivered orders as received",
                deliveredOrders.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-confirm delivered orders as received");
        }
    }
}
