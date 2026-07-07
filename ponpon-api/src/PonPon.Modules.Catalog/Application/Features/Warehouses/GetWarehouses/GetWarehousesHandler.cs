using PonPon.Modules.Catalog.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.Warehouses.GetWarehouses;

public sealed class GetWarehousesHandler
{
    private readonly IWarehouseRepository _warehouses;

    public GetWarehousesHandler(IWarehouseRepository warehouses)
    {
        _warehouses = warehouses;
    }

    public async Task<IReadOnlyCollection<WarehouseResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = await _warehouses.GetAllAsync(cancellationToken);
        return warehouses
            .Select(w => new WarehouseResponse(w.ZortWarehouseId, w.Code, w.Name, w.Address))
            .ToArray();
    }
}
