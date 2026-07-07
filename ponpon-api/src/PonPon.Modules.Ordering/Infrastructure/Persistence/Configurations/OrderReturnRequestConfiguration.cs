using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Ordering.Domain.Returns;
using PonPon.Modules.Ordering.Domain.Orders;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderReturnRequestConfiguration : IEntityTypeConfiguration<OrderReturnRequest>
{
    public void Configure(EntityTypeBuilder<OrderReturnRequest> builder)
    {
        builder.ToTable("order_return_requests");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EvidenceImageUrlsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AdminNote).HasMaxLength(2000);
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.HasOne<Order>()
            .WithOne()
            .HasForeignKey<OrderReturnRequest>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
