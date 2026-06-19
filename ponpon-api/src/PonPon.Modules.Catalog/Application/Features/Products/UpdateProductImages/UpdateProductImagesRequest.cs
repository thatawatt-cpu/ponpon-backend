namespace PonPon.Modules.Catalog.Application.Features.Products.UpdateProductImages;

public sealed record UpdateProductImagesRequest(IReadOnlyList<ProductImageItemRequest> Images);

public sealed record ProductImageItemRequest(string Url, int SortOrder, bool IsPrimary);
