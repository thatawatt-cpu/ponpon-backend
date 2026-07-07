using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.SyncRuns;
using PonPon.Modules.Ordering.Domain.Returns;

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
    public DbSet<OrderSyncRun> OrderSyncRuns => Set<OrderSyncRun>();
    public DbSet<OrderReturnRequest> OrderReturnRequests => Set<OrderReturnRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}
