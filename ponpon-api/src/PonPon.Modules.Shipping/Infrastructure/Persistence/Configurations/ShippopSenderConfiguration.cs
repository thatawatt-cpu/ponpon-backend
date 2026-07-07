using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Shipping.Domain;

namespace PonPon.Modules.Shipping.Infrastructure.Persistence.Configurations;

public sealed class ShippopSenderConfiguration : IEntityTypeConfiguration<ShippopSender>
{
    public void Configure(EntityTypeBuilder<ShippopSender> builder)
    {
        builder.ToTable("shippop_senders");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.District).HasMaxLength(128).IsRequired();
        builder.Property(x => x.State).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Province).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Postcode).HasMaxLength(5).IsRequired();
    }
}
