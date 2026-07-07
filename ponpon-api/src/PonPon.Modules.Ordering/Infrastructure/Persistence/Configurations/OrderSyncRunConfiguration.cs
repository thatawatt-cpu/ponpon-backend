using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Ordering.Domain.SyncRuns;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderSyncRunConfiguration : IEntityTypeConfiguration<OrderSyncRun>
{
    public void Configure(EntityTypeBuilder<OrderSyncRun> builder)
    {
        builder.ToTable("order_sync_runs");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.BackgroundJobId).HasMaxLength(128);
        builder.Property(x => x.ErrorsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.RequestedAtUtc);
        builder.HasIndex(x => x.Status);
    }
}
