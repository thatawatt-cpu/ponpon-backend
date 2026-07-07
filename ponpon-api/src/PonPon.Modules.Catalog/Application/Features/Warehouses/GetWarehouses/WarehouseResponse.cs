namespace PonPon.Modules.Catalog.Application.Features.Warehouses.GetWarehouses;

public sealed record WarehouseResponse(long ZortWarehouseId, string Code, string Name, string? Address);
