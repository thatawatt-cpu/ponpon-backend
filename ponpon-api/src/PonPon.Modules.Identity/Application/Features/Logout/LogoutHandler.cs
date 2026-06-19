using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Identity.Application.Features.Logout;

public sealed class LogoutHandler
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutHandler(IRefreshTokenRepository refreshTokens, IJwtTokenService jwtTokenService, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return;
        }

        var token = await _refreshTokens.GetByTokenHashAsync(_jwtTokenService.HashRefreshToken(command.RefreshToken), cancellationToken);
        token?.Revoke(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
