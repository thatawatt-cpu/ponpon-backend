using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Application.Features.Shipping.CancelShippingBooking;
using PonPon.Modules.Shipping.Application.Features.Shipping.CheckShippingRates;
using PonPon.Modules.Shipping.Application.Features.Shipping.CreateShippingBooking;
using PonPon.Modules.Shipping.Application.Features.Shipping.GetShippingBooking;
using PonPon.Modules.Shipping.Application.Features.Shipping.HandleShippopWebhook;
using PonPon.Modules.Shipping.Application.Features.ShippopSender;
using PonPon.Modules.Shipping.Application.Services;
using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;
using PonPon.Modules.Shipping.Infrastructure.Persistence;
using PonPon.Modules.Shipping.Infrastructure.Persistence.Repositories;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Shipping;

public static class ShippingModule
{
    public static IServiceCollection AddShippingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ShippopOptions>(configuration.GetSection("Shippop"));

        services.AddDbContext<ShippingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ShippingDbContext.Schema)));

        services.AddScoped<IShippingShipmentRepository, ShippingShipmentRepository>();
        services.AddScoped<IShippopSenderRepository, ShippopSenderRepository>();
        services.AddScoped<IShippingUnitOfWork, ShippingUnitOfWork>();

        services.AddHttpClient<IShippopClient, ShippopClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ShippopOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        });

        services.AddScoped<CheckShippingRatesHandler>();
        services.AddScoped<CreateShippingBookingHandler>();
        services.AddScoped<GetShippingBookingHandler>();
        services.AddScoped<CancelShippingBookingHandler>();
        services.AddScoped<HandleShippopWebhookHandler>();
        services.AddScoped<GetShippopSenderHandler>();
        services.AddScoped<UpdateShippopSenderHandler>();
        services.AddScoped<IShippingBookingAutomation, ShippopBookingAutomation>();
        services.AddScoped<IShippingRateQuoteService, ShippopShippingRateQuoteService>();

        return services;
    }
}
