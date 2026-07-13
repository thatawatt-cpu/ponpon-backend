using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.FlashSales;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class FlashSaleConfiguration : IEntityTypeConfiguration<FlashSale>
{
    public void Configure(EntityTypeBuilder<FlashSale> builder)
    {
        builder.ToTable("flash_sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Slots).HasColumnType("text[]").IsRequired();
        builder.HasIndex(x => new { x.IsActive, x.StartDate, x.EndDate });
        builder.HasMany(x => x.Products).WithOne().HasForeignKey(x => x.FlashSaleId);
        builder.Navigation(x => x.Products).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
