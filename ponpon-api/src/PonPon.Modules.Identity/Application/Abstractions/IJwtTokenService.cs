using PonPon.Modules.Identity.Domain.Customers;
using PonPon.Modules.Identity.Domain.Users;

namespace PonPon.Modules.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateCustomerAccessToken(Customer customer);
    (string Token, DateTime ExpiresAtUtc) GenerateAdminAccessToken(User user, IReadOnlyCollection<string> roles);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetRefreshTokenExpiresAtUtc();
}
