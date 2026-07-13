using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Ordering.Domain.Quotes;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Configurations;

public sealed class CheckoutQuoteConfiguration : IEntityTypeConfiguration<CheckoutQuote>
{
    public void Configure(EntityTypeBuilder<CheckoutQuote> builder)
    {
        builder.ToTable("checkout_quotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CalculationHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PricingSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CalculationStatus).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.ExpiresAtUtc);
        builder.HasIndex(x => x.ClientRequestId)
            .IsUnique()
            .HasFilter("\"ClientRequestId\" IS NOT NULL");
    }
}
