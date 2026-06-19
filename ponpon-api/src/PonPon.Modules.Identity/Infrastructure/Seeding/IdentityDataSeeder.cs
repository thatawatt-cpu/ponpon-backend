using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.Users;
using PonPon.Modules.Identity.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Identity.Infrastructure.Seeding;

public sealed class IdentityDataSeeder
{
    private const string AdminRoleName = "Admin";
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;
    private readonly SeedAdminOptions _options;

    public IdentityDataSeeder(IdentityDbContext dbContext, IPasswordHasher passwordHasher, IDateTimeProvider clock, IOptions<SeedAdminOptions> options)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Email) && string.IsNullOrWhiteSpace(_options.Password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Email) || string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("SeedAdmin:Email and SeedAdmin:Password must both be configured.");
        }

        var normalizedEmail = _options.Email.Trim().ToLowerInvariant();
        var now = _clock.UtcNow;

        var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Name == AdminRoleName, cancellationToken);
        if (adminRole is null)
        {
            adminRole = new Role(AdminRoleName);
            await _dbContext.Roles.AddAsync(adminRole, cancellationToken);
        }

        var adminUser = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (adminUser is null)
        {
            adminUser = new User(
                normalizedEmail,
                _passwordHasher.Hash(_options.Password),
                string.IsNullOrWhiteSpace(_options.DisplayName) ? "PonPon Admin" : _options.DisplayName.Trim(),
                now);

            await _dbContext.Users.AddAsync(adminUser, cancellationToken);
        }

        var hasAdminRole = await _dbContext.UserRoles
            .AnyAsync(x => x.UserId == adminUser.Id && x.RoleId == adminRole.Id, cancellationToken);

        if (!hasAdminRole)
        {
            await _dbContext.UserRoles.AddAsync(new UserRole(adminUser.Id, adminRole.Id), cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
