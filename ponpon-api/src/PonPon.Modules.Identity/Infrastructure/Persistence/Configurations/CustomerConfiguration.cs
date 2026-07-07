using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Identity.Domain.Customers;

namespace PonPon.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Navigation(x => x.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsOne(x => x.LineProfile, profile =>
        {
            profile.Property(x => x.LineUserId).HasColumnName("line_user_id").HasMaxLength(128).IsRequired();
            profile.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            profile.Property(x => x.PictureUrl).HasColumnName("picture_url").HasMaxLength(1024);
            profile.Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
            profile.HasIndex(x => x.LineUserId).IsUnique();
        });
    }
}

