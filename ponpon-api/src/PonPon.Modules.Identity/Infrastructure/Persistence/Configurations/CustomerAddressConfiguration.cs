using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Identity.Domain.Customers;

namespace PonPon.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses");

        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.RecipientName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.AddressLine1).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.AddressLine2).HasMaxLength(1024);
        builder.Property(x => x.Subdistrict).HasMaxLength(128).IsRequired();
        builder.Property(x => x.District).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Province).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Postcode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(128);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.CustomerId, x.IsDefault })
            .IsUnique()
            .HasFilter("\"IsDefault\" = true AND \"IsDeleted\" = false");

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
