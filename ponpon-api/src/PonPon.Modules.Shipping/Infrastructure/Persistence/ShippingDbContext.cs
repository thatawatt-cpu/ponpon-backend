using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Shipping.Domain;

namespace PonPon.Modules.Shipping.Infrastructure.Persistence;

public sealed class ShippingDbContext : DbContext
{
    public const string Schema = "shipping";

    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options)
    {
    }

    public DbSet<ShippingShipment> Shipments => Set<ShippingShipment>();
    public DbSet<ShippingShipmentEvent> ShipmentEvents => Set<ShippingShipmentEvent>();
    public DbSet<ShippopSender> ShippopSenders => Set<ShippopSender>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShippingDbContext).Assembly);
    }
}
