using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Ordering.Domain.Orders;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(512).IsRequired();
        builder.Property(x => x.UnitText).HasMaxLength(64);
        builder.Property(x => x.Discount).HasMaxLength(64);
        builder.Property(x => x.BundleCode).HasMaxLength(128);
        builder.Property(x => x.BundleName).HasMaxLength(512);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.PricePerUnit).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalPrice).HasPrecision(18, 2);
        builder.Property(x => x.RawZortJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.Property(x => x.OptionsJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ZortProductId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.VariantId);
    }
}
