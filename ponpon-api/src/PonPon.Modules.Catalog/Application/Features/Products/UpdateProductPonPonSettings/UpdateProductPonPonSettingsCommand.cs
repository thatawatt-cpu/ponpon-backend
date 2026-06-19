namespace PonPon.Modules.Catalog.Application.Features.Products.UpdateProductPonPonSettings;

public sealed record UpdateProductPonPonSettingsCommand(
    Guid ProductId,
    string? Slug,
    decimal? OriginalPrice,
    string? PromotionBadge,
    string? Highlights,
    string? RichDescription,
    bool IsFeatured,
    bool IsBestSeller,
    bool IsOnHomepage);
