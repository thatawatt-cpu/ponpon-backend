using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.SyncRuns;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ProductSyncRunConfiguration : IEntityTypeConfiguration<ProductSyncRun>
{
    public void Configure(EntityTypeBuilder<ProductSyncRun> builder)
    {
        builder.ToTable("product_sync_runs");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.BackgroundJobId).HasMaxLength(128);
        builder.Property(x => x.ErrorsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.RequestedAtUtc);
        builder.HasIndex(x => x.Status);
    }
}
