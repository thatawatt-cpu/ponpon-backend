using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.FlashSales;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class FlashSaleProductConfiguration : IEntityTypeConfiguration<FlashSaleProduct>
{
    public void Configure(EntityTypeBuilder<FlashSaleProduct> builder)
    {
        builder.ToTable("flash_sale_products");
        builder.HasKey(x => new { x.FlashSaleId, x.ProductId });
        builder.Property(x => x.SalePrice).HasPrecision(18, 2).IsRequired();
    }
}
