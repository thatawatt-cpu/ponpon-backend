using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Shipping.Domain;

namespace PonPon.Modules.Shipping.Infrastructure.Persistence.Configurations;

public sealed class ShippingShipmentEventConfiguration : IEntityTypeConfiguration<ShippingShipmentEvent>
{
    public void Configure(EntityTypeBuilder<ShippingShipmentEvent> builder)
    {
        builder.ToTable("shipping_shipment_events");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.ShippopTrackingCode).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OrderStatus).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CourierTrackingCode).HasMaxLength(128);
        builder.Property(x => x.WebhookEventKey).HasMaxLength(64);
        builder.Property(x => x.RawPayloadJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.ShipmentId);
        builder.HasIndex(x => x.ShippopTrackingCode);
        builder.HasIndex(x => x.OrderStatus);
        builder.HasIndex(x => x.EventDateTimeUtc);
        builder.HasIndex(x => x.WebhookEventKey)
            .IsUnique()
            .HasFilter("\"WebhookEventKey\" IS NOT NULL");
    }
}
