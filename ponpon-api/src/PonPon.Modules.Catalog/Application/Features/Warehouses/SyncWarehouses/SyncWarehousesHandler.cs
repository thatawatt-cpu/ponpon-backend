using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Warehouses;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.Warehouses.SyncWarehouses;

public sealed class SyncWarehousesHandler
{
    private readonly IZortProductClient _zort;
    private readonly IWarehouseRepository _warehouses;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public SyncWarehousesHandler(
        IZortProductClient zort,
        IWarehouseRepository warehouses,
        ICatalogUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _zort = zort;
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<SyncWarehousesResponse> HandleAsync(
        SyncWarehousesCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var response = await _zort.GetWarehousesAsync(cancellationToken: cancellationToken);
        var existing = await _warehouses.GetAllAsync(cancellationToken);
        var existingByZortId = existing.ToDictionary(w => w.ZortWarehouseId);

        var created = 0;
        var updated = 0;
        var unchanged = 0;
        var toAdd = new List<Warehouse>();

        foreach (var dto in response.List)
        {
            var code = dto.Code ?? string.Empty;
            var name = dto.Name ?? string.Empty;

            if (existingByZortId.TryGetValue(dto.Id, out var warehouse))
            {
                if (warehouse.Code != code || warehouse.Name != name || warehouse.Address != dto.Address)
                {
                    warehouse.Update(code, name, dto.Address, now);
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }
            else
            {
                toAdd.Add(new Warehouse(dto.Id, code, name, dto.Address, now));
                created++;
            }
        }

        if (toAdd.Count > 0)
            await _warehouses.AddRangeAsync(toAdd, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SyncWarehousesResponse(created, updated, unchanged);
    }
}
