using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Domain.Categories;
using PonPon.Modules.Catalog.Domain.FlashSales;
using PonPon.Modules.Catalog.Domain.HomeSlides;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Domain.SyncRuns;
using PonPon.Modules.Catalog.Domain.Warehouses;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext
{
    public const string Schema = "catalog";

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FlashSale> FlashSales => Set<FlashSale>();
    public DbSet<FlashSaleProduct> FlashSaleProducts => Set<FlashSaleProduct>();
    public DbSet<FlashSaleReservation> FlashSaleReservations => Set<FlashSaleReservation>();
    public DbSet<HomeSlide> HomeSlides => Set<HomeSlide>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<ProductSyncRun> ProductSyncRuns => Set<ProductSyncRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
