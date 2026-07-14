using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Application.AdminUsers;
using PonPon.Modules.Identity.Domain.Users;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.AdminLogin;

public sealed class AdminLoginHandler
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AdminLoginValidator _validator = new();

    public AdminLoginHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminLoginResponse> HandleAsync(
        AdminLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            throw new BadRequestException(validation.Errors[0].ErrorMessage);
        }

        var user = await _users.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null
            || user.Status != UserStatus.Active
            || !_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.MarkLoggedIn(_clock.UtcNow);
        var roles = user.UserRoles.Select(x => x.Role.Name).ToArray();
        var role = roles.Contains(AdminUserManagementService.OwnerRole)
            ? AdminUserManagementService.OwnerRole
            : roles.FirstOrDefault() ?? AdminUserManagementService.StaffRole;
        var permissions = AdminUserManagementService.ParsePermissions(user.PermissionsJson);
        var accessToken = _jwtTokenService.GenerateAdminAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        await _refreshTokens.AddAsync(
            new Domain.RefreshTokens.RefreshToken(
                _jwtTokenService.HashRefreshToken(refreshToken),
                user.Id,
                "Admin",
                _jwtTokenService.GetRefreshTokenExpiresAtUtc(),
                _clock.UtcNow),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminLoginResponse(
            accessToken.Token,
            refreshToken,
            accessToken.ExpiresAtUtc,
            new AdminProfileResponse(user.Id, user.Email, user.DisplayName, role, permissions, roles));
    }
}
