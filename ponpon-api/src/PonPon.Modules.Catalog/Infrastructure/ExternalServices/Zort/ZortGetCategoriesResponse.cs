namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed record ZortGetCategoriesResponse(IReadOnlyCollection<ZortCategoryDto> Categories, string? ResCode, string? ResDesc);
