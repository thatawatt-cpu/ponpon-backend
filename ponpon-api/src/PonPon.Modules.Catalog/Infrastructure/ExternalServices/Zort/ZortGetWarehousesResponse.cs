namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed record ZortGetWarehousesResponse(
    IReadOnlyCollection<ZortWarehouseDto> List,
    int? Count,
    string? ResCode,
    string? ResDesc);
