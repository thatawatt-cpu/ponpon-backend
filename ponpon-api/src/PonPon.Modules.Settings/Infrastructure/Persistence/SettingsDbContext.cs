using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Settings.Domain;

namespace PonPon.Modules.Settings.Infrastructure.Persistence;

public sealed class SettingsDbContext : DbContext
{
    public const string Schema = "settings";

    public SettingsDbContext(DbContextOptions<SettingsDbContext> options) : base(options) { }

    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SettingsDbContext).Assembly);
    }
}
