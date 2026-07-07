using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Services;

namespace PonPon.Modules.Ordering.Application.Features.Orders.ExpireUnpaidOrders;

public sealed class ExpiredOrderCancellationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredOrderCancellationJob> _logger;

    public ExpiredOrderCancellationJob(IServiceProvider serviceProvider, ILogger<ExpiredOrderCancellationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        using var stoppingRegistration = stoppingToken.Register(timer.Dispose);
        try
        {
            while (await timer.WaitForNextTickAsync())
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await ProcessExpiredOrdersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process expired unpaid orders");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal BackgroundService shutdown.
        }
    }

    private async Task ProcessExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var zort = scope.ServiceProvider.GetRequiredService<IZortOrderClient>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrderingUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var shopRealtimeNotifications = scope.ServiceProvider.GetRequiredService<IShopRealtimeNotificationService>();
        var stockReservations = scope.ServiceProvider.GetRequiredService<OrderStockReservationService>();

        var expiredOrders = await orders.GetExpiredUnpaidAsync(
            clock.UtcNow,
            SyncOrdersFromZortHandler.LineLiffSalesChannel,
            cancellationToken);

        if (expiredOrders.Count == 0)
            return;

        _logger.LogInformation("Found {Count} expired unpaid orders to cancel", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            try
            {
                if (order.ZortOrderId > 0)
                {
                    await zort.VoidOrderAsync(order.ZortOrderId, cancellationToken);
                }

                order.MarkVoided(
                    clock.UtcNow,
                    "Payment expired before completion.",
                    "System");

                await stockReservations.ReleaseAsync(order, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await shopRealtimeNotifications.NotifyAsync(
                    new ShopRealtimeNotification(
                        order.CustomerId,
                        order.IntegrationCustomerId,
                        "payment_expired",
                        order.Id,
                        order.Number,
                        "หมดเวลาชำระเงิน",
                        "คำสั่งซื้อถูกยกเลิกเพราะไม่ได้ชำระเงินในเวลาที่กำหนด",
                        order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
                        order.PaymentStatus),
                    cancellationToken);

                _logger.LogInformation("Auto-cancelled expired order {OrderNumber}", order.Number);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-cancel expired order {OrderNumber}", order.Number);
            }
        }
    }
}
