using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Notification.Domain;

namespace PonPon.Modules.Notification.Infrastructure.Persistence.Configurations;

public sealed class ShopNotificationConfiguration : IEntityTypeConfiguration<ShopNotification>
{
    public void Configure(EntityTypeBuilder<ShopNotification> builder)
    {
        builder.ToTable("shop_notifications");
        builder.HasKey(x => x.Id);

        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LineUserId).HasMaxLength(128);
        builder.Property(x => x.OrderNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(64);
        builder.Property(x => x.TrackingNumber).HasMaxLength(128);
        builder.Property(x => x.ActionUrl).HasMaxLength(1000);

        builder.Ignore(x => x.IsRead);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.LineUserId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.ReadAtUtc);
    }
}
