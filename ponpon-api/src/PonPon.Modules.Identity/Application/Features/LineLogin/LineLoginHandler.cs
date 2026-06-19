using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.Customers;
using PonPon.Modules.Identity.Domain.RefreshTokens;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.LineLogin;

public sealed class LineLoginHandler
{
    private readonly ILineAuthService _lineAuthService;
    private readonly ICustomerRepository _customers;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LineLoginValidator _validator = new();

    public LineLoginHandler(ILineAuthService lineAuthService, ICustomerRepository customers, IRefreshTokenRepository refreshTokens, IJwtTokenService jwtTokenService, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    {
        _lineAuthService = lineAuthService;
        _customers = customers;
        _refreshTokens = refreshTokens;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<LineLoginResponse> HandleAsync(LineLoginCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            throw new BadRequestException(validation.Errors[0].ErrorMessage);
        }

        var lineProfile = await _lineAuthService.VerifyIdTokenAsync(command.IdToken, cancellationToken);
        var now = _clock.UtcNow;
        var customer = await _customers.GetByLineUserIdAsync(lineProfile.LineUserId, cancellationToken);

        if (customer is null)
        {
            customer = Customer.Create(lineProfile, now);
            await _customers.AddAsync(customer, cancellationToken);
        }
        else
        {
            customer.UpdateLineProfile(lineProfile, now);
        }

        var accessToken = _jwtTokenService.GenerateCustomerAccessToken(customer);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        await _refreshTokens.AddAsync(new PonPon.Modules.Identity.Domain.RefreshTokens.RefreshToken(_jwtTokenService.HashRefreshToken(refreshToken), customer.Id, "Customer", _jwtTokenService.GetRefreshTokenExpiresAtUtc(), now), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LineLoginResponse(accessToken.Token, refreshToken, accessToken.ExpiresAtUtc, new CustomerProfileResponse(customer.Id, customer.LineProfile.LineUserId, customer.LineProfile.DisplayName, customer.LineProfile.PictureUrl, customer.LineProfile.Email));
    }
}
