namespace PonPon.Modules.Identity.Application.Features.LineLogin;

public sealed record LineLoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    CustomerProfileResponse Customer);

public sealed record CustomerProfileResponse(Guid CustomerId, string LineUserId, string DisplayName, string? PictureUrl, string? Email);
