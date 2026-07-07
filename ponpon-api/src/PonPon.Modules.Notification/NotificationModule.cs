using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Notification.Infrastructure.Persistence;
using PonPon.Modules.Notification.Line;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Notification;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationDbContext.Schema)));

        services.Configure<LineNotificationOptions>(configuration.GetSection("Line"));
        services.AddHttpClient<IOrderCancellationNotifier, LineOrderCancellationNotifier>();
        services.AddHttpClient<ILineOrderNotificationService, LineOrderNotificationService>();
        return services;
    }
}
