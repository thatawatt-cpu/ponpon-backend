using Microsoft.Extensions.DependencyInjection;
using PonPon.Modules.Identity.Infrastructure.Seeding;

namespace PonPon.Modules.Identity;

public static class IdentityModuleSeederExtensions
{
    public static async Task SeedIdentityModuleAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
