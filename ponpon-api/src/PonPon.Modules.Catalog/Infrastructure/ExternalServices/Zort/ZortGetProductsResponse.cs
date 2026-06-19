namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed record ZortGetProductsResponse(IReadOnlyCollection<ZortProductDto> Products, int? Count, int? Page, int? Limit, string? ResCode, string? ResDesc);
