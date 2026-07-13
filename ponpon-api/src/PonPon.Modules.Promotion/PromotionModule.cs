using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Infrastructure;
using PonPon.Shared.Application.Abstractions;

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
        services.AddMemoryCache();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IShopCouponService, ShopCouponService>();
        services.AddScoped<ICustomerProfileSummaryProvider, CouponProfileSummaryProvider>();
        services.AddScoped<ICouponBulkGenerationJobService, CouponBulkGenerationJobService>();
        services.AddScoped<CouponBulkGenerationBackgroundJob>();
        services.AddScoped<ICouponCampaignService, CouponCampaignService>();
        services.AddScoped<IPromotionService, PromotionService>();
        return services;
    }
}
