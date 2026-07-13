namespace PonPon.Modules.Identity.Application.Features.GetMe;

public sealed record MeResponse(
    Guid Id,
    string UserType,
    string DisplayName,
    string? Email,
    string? PictureUrl,
    IReadOnlyCollection<string> Roles,
    int WishlistCount = 0,
    int CouponCount = 0,
    int RecentlyViewedCount = 0);
