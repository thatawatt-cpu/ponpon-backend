namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed record ZortStockItemDto(long ProductId, string? Sku, int Stock, string? WarehouseCode);
