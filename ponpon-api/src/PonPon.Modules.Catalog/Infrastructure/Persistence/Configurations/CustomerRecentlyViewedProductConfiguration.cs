using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.CustomerEngagement;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class CustomerRecentlyViewedProductConfiguration : IEntityTypeConfiguration<CustomerRecentlyViewedProduct>
{
    public void Configure(EntityTypeBuilder<CustomerRecentlyViewedProduct> builder)
    {
        builder.ToTable("customer_recently_viewed_products");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.HasIndex(x => new { x.CustomerId, x.ProductId }).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.ViewedAtUtc });
    }
}
