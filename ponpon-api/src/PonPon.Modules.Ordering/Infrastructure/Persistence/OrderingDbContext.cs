using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.Quotes;
using PonPon.Modules.Ordering.Domain.SyncRuns;
using PonPon.Modules.Ordering.Domain.Returns;
using PonPon.Modules.Ordering.Infrastructure.Persistence.ReadModels;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence;

public sealed class OrderingDbContext : DbContext
{
    public const string Schema = "ordering";

    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();
    public DbSet<CheckoutQuote> CheckoutQuotes => Set<CheckoutQuote>();
    public DbSet<OrderSyncRun> OrderSyncRuns => Set<OrderSyncRun>();
    public DbSet<OrderReturnRequest> OrderReturnRequests => Set<OrderReturnRequest>();
    public DbSet<ReviewReadModel> ReviewReadModels => Set<ReviewReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);

        modelBuilder.Entity<ReviewReadModel>(builder =>
        {
            builder.ToTable("reviews", "reviews", t => t.ExcludeFromMigrations());
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OrderItemId);
            builder.Property(x => x.DeletedAtUtc);
        });
    }
}
