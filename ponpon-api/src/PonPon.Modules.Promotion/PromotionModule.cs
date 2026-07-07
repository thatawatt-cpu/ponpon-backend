using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Infrastructure;

namespace PonPon.Modules.Promotion;

public static class PromotionModule
{
    public static IServiceCollection AddPromotionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PromotionDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    PromotionDbContext.Schema)));
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<ICouponBulkGenerationJobService, CouponBulkGenerationJobService>();
        services.AddScoped<CouponBulkGenerationBackgroundJob>();
        services.AddScoped<ICouponCampaignService, CouponCampaignService>();
        services.AddScoped<IPromotionService, PromotionService>();
        return services;
    }
}
