using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Identity.Application.AdminUsers;
using PonPon.Modules.Identity.Infrastructure.Persistence;

namespace PonPon.Modules.Identity.Application.Features.GetAdminSetupStatus;

public sealed class GetAdminSetupStatusHandler
{
    private readonly IdentityDbContext _dbContext;

    public GetAdminSetupStatusHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminSetupStatusResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var hasAdmin = await _dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(
                x => x.Role.Name == AdminUserManagementService.OwnerRole
                     || x.Role.Name == AdminUserManagementService.AdminRole,
                cancellationToken);

        return new AdminSetupStatusResponse(hasAdmin);
    }
}

public sealed record AdminSetupStatusResponse(bool HasAdmin);
