using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Reviews.Domain;

namespace PonPon.Modules.Reviews.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Comment).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.IsAnonymous).HasDefaultValue(false);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.VariantId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DeletedAtUtc);
        builder.HasIndex(x => x.OrderItemId)
            .IsUnique()
            .HasFilter("\"DeletedAtUtc\" IS NULL");
        builder.HasMany(x => x.Media)
            .WithOne()
            .HasForeignKey(x => x.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Media).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
