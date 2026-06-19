using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Domain.Categories;
using PonPon.Modules.Catalog.Domain.FlashSales;
using PonPon.Modules.Catalog.Domain.HomeSlides;
using PonPon.Modules.Catalog.Domain.Products;

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
    public DbSet<HomeSlide> HomeSlides => Set<HomeSlide>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
