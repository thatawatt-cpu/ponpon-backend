namespace PonPon.Modules.Identity.Application.Features.AdminLogin;

public sealed record AdminLoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, AdminProfileResponse Admin);

public sealed record AdminProfileResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<string> Roles);
