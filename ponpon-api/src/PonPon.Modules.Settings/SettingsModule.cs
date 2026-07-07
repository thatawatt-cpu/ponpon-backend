using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PonPon.Modules.Settings.Application.Abstractions;
using PonPon.Modules.Settings.Application.Features.GetZortWebhook;
using PonPon.Modules.Settings.Application.Features.GetZortWebhookFromZort;
using PonPon.Modules.Settings.Application.Features.RegisterZortWebhook;
using PonPon.Modules.Settings.Infrastructure.Persistence;
using PonPon.Modules.Settings.Infrastructure.Persistence.Repositories;

namespace PonPon.Modules.Settings;

public static class SettingsModule
{
    public static IServiceCollection AddSettingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SettingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", SettingsDbContext.Schema)));

        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ISettingsUnitOfWork, SettingsUnitOfWork>();

        services.AddScoped<GetZortWebhookHandler>();
        services.AddScoped<GetZortWebhookFromZortHandler>();
        services.AddScoped<RegisterZortWebhookHandler>();

        return services;
    }
}
