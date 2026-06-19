using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.HomeSlides;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class HomeSlideConfiguration : IEntityTypeConfiguration<HomeSlide>
{
    public void Configure(EntityTypeBuilder<HomeSlide> builder)
    {
        builder.ToTable("home_slides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Image).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Badge).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.LinkUrl).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.CtaLabel).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.SortOrder).IsUnique();
    }
}
