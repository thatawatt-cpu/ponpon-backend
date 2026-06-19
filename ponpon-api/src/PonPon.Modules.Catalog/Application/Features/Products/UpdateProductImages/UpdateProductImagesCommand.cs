namespace PonPon.Modules.Catalog.Application.Features.Products.UpdateProductImages;

public sealed record UpdateProductImagesCommand(
    Guid ProductId,
    IReadOnlyList<(string Url, int SortOrder, bool IsPrimary)> Images);
