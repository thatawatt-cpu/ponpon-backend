using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Notification.Domain;

namespace PonPon.Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public const string Schema = "notification";

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<ShopNotification> ShopNotifications => Set<ShopNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
