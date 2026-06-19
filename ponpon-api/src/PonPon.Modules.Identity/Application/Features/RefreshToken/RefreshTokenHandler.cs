using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.RefreshTokens;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.RefreshToken;

public sealed class RefreshTokenHandler
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ICustomerRepository _customers;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenHandler(IRefreshTokenRepository refreshTokens, ICustomerRepository customers, IUserRepository users, IJwtTokenService jwtTokenService, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _customers = customers;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshTokenResponse> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var oldToken = await _refreshTokens.GetByTokenHashAsync(_jwtTokenService.HashRefreshToken(command.RefreshToken), cancellationToken);
        if (oldToken is null || !oldToken.IsActive(_clock.UtcNow))
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        oldToken.Revoke(_clock.UtcNow);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        await _refreshTokens.AddAsync(new PonPon.Modules.Identity.Domain.RefreshTokens.RefreshToken(_jwtTokenService.HashRefreshToken(newRefreshToken), oldToken.SubjectId, oldToken.UserType, _jwtTokenService.GetRefreshTokenExpiresAtUtc(), _clock.UtcNow), cancellationToken);

        (string Token, DateTime ExpiresAtUtc) accessToken = oldToken.UserType switch
        {
            "Customer" => _jwtTokenService.GenerateCustomerAccessToken(await _customers.GetByIdAsync(oldToken.SubjectId, cancellationToken) ?? throw new UnauthorizedException("Customer no longer exists.")),
            "Admin" => CreateAdminAccessToken(await _users.GetByIdAsync(oldToken.SubjectId, cancellationToken) ?? throw new UnauthorizedException("Admin user no longer exists.")),
            _ => throw new UnauthorizedException("Invalid refresh token subject.")
        };

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new RefreshTokenResponse(accessToken.Token, newRefreshToken, accessToken.ExpiresAtUtc);
    }

    private (string Token, DateTime ExpiresAtUtc) CreateAdminAccessToken(Domain.Users.User user)
    {
        return _jwtTokenService.GenerateAdminAccessToken(user, user.UserRoles.Select(x => x.Role.Name).ToArray());
    }
}
