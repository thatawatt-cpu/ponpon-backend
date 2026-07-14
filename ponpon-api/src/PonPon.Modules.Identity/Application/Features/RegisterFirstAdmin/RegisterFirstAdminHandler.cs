using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Application.AdminUsers;
using PonPon.Modules.Identity.Domain.Users;
using PonPon.Modules.Identity.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.RegisterFirstAdmin;

public sealed class RegisterFirstAdminHandler
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;
    private readonly RegisterFirstAdminValidator _validator = new();

    public RegisterFirstAdminHandler(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<RegisterFirstAdminResponse> HandleAsync(
        RegisterFirstAdminCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new BadRequestException(validation.Errors[0].ErrorMessage);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await HasAdminAsync(cancellationToken))
            throw new BadRequestException(
                "Admin registration is closed because an admin already exists.",
                "admin_already_exists");

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        if (await _dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
            throw new BadRequestException("Email is already registered.");

        var role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Name == AdminUserManagementService.OwnerRole, cancellationToken);
        if (role is null)
        {
            role = new Role(AdminUserManagementService.OwnerRole);
            await _dbContext.Roles.AddAsync(role, cancellationToken);
        }

        var displayName = string.IsNullOrWhiteSpace(command.DisplayName)
            ? "PonPon Admin"
            : command.DisplayName.Trim();
        var user = new User(
            normalizedEmail,
            _passwordHasher.Hash(command.Password),
            displayName,
            AdminUserManagementService.SerializePermissions(AdminUserPermissions.OwnerPermissions),
            _clock.UtcNow);

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContext.UserRoles.AddAsync(new UserRole(user.Id, role.Id), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RegisterFirstAdminResponse(user.Id, user.Email, user.DisplayName);
    }

    private Task<bool> HasAdminAsync(CancellationToken cancellationToken)
        => _dbContext.UserRoles.AnyAsync(
            x => x.Role.Name == AdminUserManagementService.OwnerRole
                 || x.Role.Name == AdminUserManagementService.AdminRole,
            cancellationToken);
}
