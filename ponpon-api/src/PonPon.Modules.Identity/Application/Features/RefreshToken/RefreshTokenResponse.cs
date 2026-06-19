namespace PonPon.Modules.Identity.Application.Features.RefreshToken;

public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
