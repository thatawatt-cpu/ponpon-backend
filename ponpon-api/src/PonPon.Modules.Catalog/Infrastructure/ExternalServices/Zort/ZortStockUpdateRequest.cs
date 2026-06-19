namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed record ZortStockUpdateRequest(IReadOnlyCollection<ZortStockItemDto> Items);
