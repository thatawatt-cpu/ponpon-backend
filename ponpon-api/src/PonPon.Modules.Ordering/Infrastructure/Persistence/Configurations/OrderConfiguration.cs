using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Ordering.Domain.Orders;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Number).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CustomerCode).HasMaxLength(128);
        builder.Property(x => x.CustomerName).HasMaxLength(512);
        builder.Property(x => x.CustomerIdNumber).HasMaxLength(128);
        builder.Property(x => x.CustomerEmail).HasMaxLength(320);
        builder.Property(x => x.CustomerPhone).HasMaxLength(128);
        builder.Property(x => x.CustomerAddress).HasMaxLength(4096);
        builder.Property(x => x.CancellationReason).HasMaxLength(2000);
        builder.Property(x => x.CanceledBy).HasMaxLength(64);
        builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PaymentStatus).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OmiseChargeId).HasMaxLength(128);
        builder.Property(x => x.CheckoutPaymentMethod).HasMaxLength(128);
        builder.Property(x => x.OmiseRefundId).HasMaxLength(128);
        builder.Property(x => x.OmiseRefundStatus).HasMaxLength(64);
        builder.Property(x => x.ShippingChannel).HasMaxLength(128);
        builder.Property(x => x.ShippingName).HasMaxLength(512);
        builder.Property(x => x.ShippingAddress).HasMaxLength(4096);
        builder.Property(x => x.ShippingPhone).HasMaxLength(128);
        builder.Property(x => x.TrackingNo).HasMaxLength(256);
        builder.Property(x => x.Reference).HasMaxLength(512);
        builder.Property(x => x.Description).HasMaxLength(4096);
        builder.Property(x => x.SalesChannel).HasMaxLength(128).IsRequired();
        builder.Property(x => x.IntegrationCustomerId).HasMaxLength(256);
        builder.Property(x => x.IntegrationCustomer).HasMaxLength(512);
        builder.Property(x => x.WarehouseCode).HasMaxLength(128);
        builder.Property(x => x.Currency).HasMaxLength(16);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.ShippingAmount).HasPrecision(18, 2);
        builder.Property(x => x.PaymentAmount).HasPrecision(18, 2);
        builder.Property(x => x.RefundedAmount).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TagsJson).HasColumnType("jsonb");
        builder.Property(x => x.RawZortJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PricingSnapshotJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.ZortOrderId).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.CustomerId, x.OrderDate })
            .HasDatabaseName("IX_orders_customer_order_date");
        builder.HasIndex(x => new { x.CustomerId, x.PaymentStatus, x.Status })
            .HasDatabaseName("IX_orders_customer_payment_status");
        builder.HasIndex(x => x.Number);
        builder.HasIndex(x => new { x.SalesChannel, x.OrderDate });
        builder.HasIndex(x => x.IntegrationCustomerId);
        builder.HasIndex(x => new { x.Status, x.PaymentStatus });
        builder.HasIndex(x => x.DeliveredNotificationSentAtUtc)
            .HasFilter("\"DeliveredNotificationSentAtUtc\" IS NOT NULL");
        builder.HasIndex(x => x.ReceivedAtUtc).HasFilter("\"ReceivedAtUtc\" IS NOT NULL");
        builder.HasIndex(x => x.PaymentExpiresAt).HasFilter("\"PaymentExpiresAt\" IS NOT NULL");
        builder.HasIndex(x => x.OmiseChargeId)
            .IsUnique()
            .HasFilter("\"OmiseChargeId\" IS NOT NULL");
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
