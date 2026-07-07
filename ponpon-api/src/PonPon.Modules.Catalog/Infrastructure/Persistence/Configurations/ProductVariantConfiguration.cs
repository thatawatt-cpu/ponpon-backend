using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsVisibleToCustomer);
        builder.Property(x => x.BaseSku).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
        builder.Property(x => x.VariantCode).HasMaxLength(64);
        builder.Property(x => x.Barcode).HasMaxLength(128);
        builder.Property(x => x.UnitText).HasMaxLength(64);
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.SellPrice).HasPrecision(18, 2);
        builder.Property(x => x.PurchasePrice).HasPrecision(18, 2);
        builder.Property(x => x.OptionsJson).HasColumnType("jsonb");
        builder.Property(x => x.RawZortJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.ZortProductId).IsUnique();
        builder.HasIndex(x => new { x.ProductId, x.VariantCode });
    }
}
