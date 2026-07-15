using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PonPon.Api.Infrastructure;
using PonPon.Api.Realtime;
using PonPon.Api.Services;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Infrastructure.Messaging;

namespace PonPon.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPonPonServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddPonPonCors(configuration);
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.TryAddScoped<IOrderCancellationNotifier, NoopOrderCancellationNotifier>();
        services.TryAddScoped<ILineOrderNotificationService, NoopLineOrderNotificationService>();
        services.TryAddScoped<IShopRealtimeNotificationService, NoopShopRealtimeNotificationService>();
        services.AddSignalR();
        services.AddScoped<IShopRealtimeNotificationService, SignalRShopRealtimeNotificationService>();
        services.AddSingleton<BackgroundTaskQueue>();
        services.AddSingleton<IBackgroundTaskQueue>(sp => sp.GetRequiredService<BackgroundTaskQueue>());
        services.AddHostedService<BackgroundTaskProcessor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddHangfire(hangfire => hangfire
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = configuration.GetHangfireWorkerCount();
            options.Queues = ["default"];
        });
        services.AddScoped<IPersistentBackgroundJobClient, HangfirePersistentBackgroundJobClient>();

        services.AddPonPonAuthentication(configuration);
        return services;
    }
}
