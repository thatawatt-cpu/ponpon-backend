using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Shipping.Domain;

namespace PonPon.Modules.Shipping.Infrastructure.Persistence.Configurations;

public sealed class ShippingShipmentConfiguration : IEntityTypeConfiguration<ShippingShipment>
{
    public void Configure(EntityTypeBuilder<ShippingShipment> builder)
    {
        builder.ToTable("shipping_shipments");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.OrderNumber).HasMaxLength(128);
        builder.Property(x => x.ShippopTrackingCode).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CourierTrackingCode).HasMaxLength(128);
        builder.Property(x => x.CourierCode).HasMaxLength(64);
        builder.Property(x => x.CourierName).HasMaxLength(256);
        builder.Property(x => x.ShippingStatus).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.LabelUrl).HasMaxLength(2048);
        builder.Property(x => x.RawBookingJson).HasColumnType("jsonb");
        builder.Property(x => x.LastWebhookJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.ShippopTrackingCode).IsUnique();
        builder.HasIndex(x => x.OrderId).IsUnique().HasFilter("\"OrderId\" IS NOT NULL");
        builder.HasIndex(x => x.CourierTrackingCode);
        builder.HasIndex(x => x.OrderNumber);
        builder.HasIndex(x => x.ShippingStatus);
        builder.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Events).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
