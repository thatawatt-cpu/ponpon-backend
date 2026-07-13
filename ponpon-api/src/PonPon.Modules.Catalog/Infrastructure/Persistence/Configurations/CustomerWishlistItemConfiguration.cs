using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.CustomerEngagement;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class CustomerWishlistItemConfiguration : IEntityTypeConfiguration<CustomerWishlistItem>
{
    public void Configure(EntityTypeBuilder<CustomerWishlistItem> builder)
    {
        builder.ToTable("customer_wishlist_items");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.HasIndex(x => new { x.CustomerId, x.ProductId }).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc });
    }
}
