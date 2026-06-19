using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsVisibleToCustomer);
        builder.Property(x => x.Name).HasMaxLength(512).IsRequired();
        builder.Property(x => x.BaseSku).HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(4096);
        builder.Property(x => x.Barcode).HasMaxLength(128);
        builder.Property(x => x.UnitText).HasMaxLength(64);
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.Property(x => x.CategoryName).HasMaxLength(512);
        builder.Property(x => x.SubCategoryName).HasMaxLength(512);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.SellPrice).HasPrecision(18, 2);
        builder.Property(x => x.PurchasePrice).HasPrecision(18, 2);
        builder.Property(x => x.Weight).HasPrecision(18, 3);
        builder.Property(x => x.Height).HasPrecision(18, 3);
        builder.Property(x => x.Length).HasPrecision(18, 3);
        builder.Property(x => x.Width).HasPrecision(18, 3);
        builder.Property(x => x.RawZortJson).HasColumnType("jsonb");
        builder.Property(x => x.Slug).HasMaxLength(256);
        builder.Property(x => x.OriginalPrice).HasPrecision(18, 2);
        builder.Property(x => x.PromotionBadge).HasMaxLength(64);
        builder.Property(x => x.Highlights).HasMaxLength(2048);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.ZortProductId);
        builder.HasIndex(x => x.BaseSku);
        builder.HasMany(x => x.Images).WithOne().HasForeignKey(x => x.ProductId);
        builder.HasMany(x => x.Variants).WithOne().HasForeignKey(x => x.ProductId);
        builder.Navigation(x => x.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
