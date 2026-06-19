namespace PonPon.Modules.Catalog.Application.Features.Products.UpdateProductPonPonSettings;

public sealed record UpdateProductPonPonSettingsRequest(
    string? Slug,
    decimal? OriginalPrice,
    string? PromotionBadge,
    string? Highlights,
    string? RichDescription,
    bool IsFeatured,
    bool IsBestSeller,
    bool IsOnHomepage);
