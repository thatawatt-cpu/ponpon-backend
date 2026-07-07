using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.PreviewPricing;
using PonPon.Modules.Ordering.Application.Features.Orders.ApproveManualRefund;
using PonPon.Modules.Ordering.Application.Features.Orders.CancelMyOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.CancelOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.GetOrderById;
using PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;
using PonPon.Modules.Ordering.Application.Features.Orders.HandleZortWebhook;
using PonPon.Modules.Ordering.Infrastructure;
using PonPon.Shared.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncPendingOrderToZort;
using PonPon.Modules.Ordering.Application.Features.Orders.ExpireUnpaidOrders;
using PonPon.Modules.Ordering.Application.Services;
using PonPon.Modules.Ordering.Application.Features.Orders.ReturnOrder;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Modules.Ordering.Infrastructure.Persistence;
using PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;
using PonPon.Modules.Ordering.Application.Pricing;

namespace PonPon.Modules.Ordering;

public static class OrderingModule
{
    public static IServiceCollection AddOrderingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZortOrderOptions>(configuration.GetSection("Zort"));
        services.Configure<PricingOptions>(configuration.GetSection("Pricing"));

        services.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", OrderingDbContext.Schema)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderSyncRunRepository, OrderSyncRunRepository>();
        services.AddScoped<IOrderReturnRequestRepository, OrderReturnRequestRepository>();
        services.AddScoped<IOrderingUnitOfWork, OrderingUnitOfWork>();
        services.AddScoped<IOrderShippingStatusUpdater, OrderShippingStatusUpdater>();
        services.AddScoped<OrderStockReservationService>();
        services.AddScoped<PricingPipeline>();
        services.AddScoped<IPricingStep, FlashSalePricingStep>();
        services.AddScoped<IPricingStep, AutoPromotionPricingStep>();
        services.AddScoped<IPricingStep, CouponPricingStep>();
        services.AddScoped<IPricingStep, VatPricingStep>();
        services.AddScoped<IPricingStep, FinalizePricingStep>();

        services.AddHttpClient<IZortOrderClient, ZortOrderClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ZortOrderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(75);
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        });

        services.AddScoped<GetOrdersHandler>();
        services.AddScoped<AddOrderHandler>();
        services.AddScoped<PreviewPricingHandler>();
        services.AddScoped<GetOrderByIdHandler>();
        services.AddScoped<GetMyOrdersHandler>();
        services.AddScoped<GetMyOrderByIdHandler>();
        services.AddScoped<SyncOrdersFromZortHandler>();
        services.AddScoped<OrderSyncBackgroundJob>();
        services.AddScoped<SyncPendingOrderToZortHandler>();
        services.AddScoped<CancelOrderHandler>();
        services.AddScoped<ApproveManualRefundHandler>();
        services.AddScoped<CancelMyOrderHandler>();
        services.AddScoped<HandleZortWebhookHandler>();
        services.AddScoped<CreateOrderReturnRequestHandler>();
        services.AddScoped<GetMyOrderReturnRequestHandler>();
        services.AddScoped<GetOrderReturnRequestHandler>();
        services.AddScoped<UpdateOrderReturnRequestHandler>();
        services.AddScoped<IZortWebhookRegistrar, ZortWebhookRegistrar>();

        services.AddHostedService<ExpiredOrderCancellationJob>();
        services.AddHostedService<PendingZortOrderSyncJob>();

        return services;
    }
}
