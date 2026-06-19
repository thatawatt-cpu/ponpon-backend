using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.CancelMyOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.CancelOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.GetOrderById;
using PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;
using PonPon.Modules.Ordering.Application.Features.Orders.HandleZortWebhook;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Modules.Ordering.Infrastructure.Persistence;
using PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;

namespace PonPon.Modules.Ordering;

public static class OrderingModule
{
    public static IServiceCollection AddOrderingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZortOrderOptions>(configuration.GetSection("Zort"));

        services.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", OrderingDbContext.Schema)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderingUnitOfWork, OrderingUnitOfWork>();

        services.AddHttpClient<IZortOrderClient, ZortOrderClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ZortOrderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddScoped<GetOrdersHandler>();
        services.AddScoped<AddOrderHandler>();
        services.AddScoped<GetOrderByIdHandler>();
        services.AddScoped<GetMyOrdersHandler>();
        services.AddScoped<GetMyOrderByIdHandler>();
        services.AddScoped<SyncOrdersFromZortHandler>();
        services.AddScoped<CancelOrderHandler>();
        services.AddScoped<CancelMyOrderHandler>();
        services.AddScoped<HandleZortWebhookHandler>();

        return services;
    }
}
