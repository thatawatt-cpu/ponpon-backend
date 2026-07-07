using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.Categories.GetCategories;
using PonPon.Modules.Catalog.Application.Features.FlashSales.CreateFlashSale;
using PonPon.Modules.Catalog.Application.Features.FlashSales.DeleteFlashSale;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSaleById;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetActiveFlashSale;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;
using PonPon.Modules.Catalog.Application.Features.FlashSales.UpdateFlashSale;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.CreateHomeSlide;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.DeleteHomeSlide;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.GetHomeSlides;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.GetPublishedHomeSlides;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.ReorderHomeSlides;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.UpdateHomeSlide;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductById;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductBySlug;
using PonPon.Modules.Catalog.Application.Features.Products.GetProducts;
using PonPon.Modules.Catalog.Application.Features.Products.SyncProductsFromZort;
using PonPon.Modules.Catalog.Application.Features.Products.HandleZortProductWebhook;
using PonPon.Modules.Catalog.Application.Features.Products.SyncSingleProductFromZort;
using PonPon.Modules.Catalog.Application.Features.Products.UploadProductImage;
using PonPon.Modules.Catalog.Application.Features.Products.UpdateProductImages;
using PonPon.Modules.Catalog.Application.Features.Products.UpdateProductPonPonSettings;
using PonPon.Modules.Catalog.Application.Features.Products.UpdateProductVisibility;
using PonPon.Modules.Catalog.Application.Features.Uploads;
using PonPon.Modules.Catalog.Application.Features.Warehouses.GetWarehouses;
using PonPon.Modules.Catalog.Application.Features.Warehouses.SyncWarehouses;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;
using PonPon.Modules.Catalog.Infrastructure.Persistence;
using PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;

namespace PonPon.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZortOptions>(configuration.GetSection("Zort"));
        services.Configure<SupabaseOptions>(configuration.GetSection("Supabase"));

        services.AddHttpClient<ISupabaseStorageService, SupabaseStorageService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<SupabaseOptions>>().Value;
            client.BaseAddress = new Uri(options.Url.TrimEnd('/') + "/");
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        });

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", CatalogDbContext.Schema)));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IFlashSaleRepository, FlashSaleRepository>();
        services.AddScoped<IHomeSlideRepository, HomeSlideRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IProductSyncRunRepository, ProductSyncRunRepository>();
        services.AddScoped<ICatalogUnitOfWork, CatalogUnitOfWork>();

        services.AddHttpClient<IZortProductClient, ZortProductClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ZortOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        });

        services.AddScoped<IRichTextImageProcessor, RichTextImageProcessor>();

        services.AddScoped<GetProductsHandler>();
        services.AddScoped<GetProductByIdHandler>();
        services.AddScoped<GetProductBySlugHandler>();
        services.AddScoped<SyncProductsFromZortHandler>();
        services.AddScoped<ProductSyncBackgroundJob>();
        services.AddScoped<SyncSingleProductFromZortHandler>();
        services.AddScoped<HandleZortProductWebhookHandler>();
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<UpdateProductVisibilityHandler>();
        services.AddScoped<UploadProductImageHandler>();
        services.AddScoped<UpdateProductImagesHandler>();
        services.AddScoped<UpdateProductPonPonSettingsHandler>();
        services.AddScoped<GetActiveFlashSaleHandler>();
        services.AddScoped<GetFlashSalesHandler>();
        services.AddScoped<GetFlashSaleByIdHandler>();
        services.AddScoped<CreateFlashSaleHandler>();
        services.AddScoped<UpdateFlashSaleHandler>();
        services.AddScoped<DeleteFlashSaleHandler>();
        services.AddScoped<GetHomeSlidesHandler>();
        services.AddScoped<GetPublishedHomeSlidesHandler>();
        services.AddScoped<CreateHomeSlideHandler>();
        services.AddScoped<UpdateHomeSlideHandler>();
        services.AddScoped<DeleteHomeSlideHandler>();
        services.AddScoped<ReorderHomeSlidesHandler>();
        services.AddScoped<UploadAdminFileHandler>();
        services.AddScoped<GetWarehousesHandler>();
        services.AddScoped<SyncWarehousesHandler>();

        return services;
    }
}
