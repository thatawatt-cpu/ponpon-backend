using PonPon.Shared.Domain;

namespace PonPon.Modules.Catalog.Domain.Warehouses;

public sealed class Warehouse : Entity, IAuditableEntity
{
    private Warehouse()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public Warehouse(long zortWarehouseId, string code, string name, string? address, DateTime now)
    {
        ZortWarehouseId = zortWarehouseId;
        Code = code;
        Name = name;
        Address = address;
        CreatedAtUtc = now;
    }

    public long ZortWarehouseId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Address { get; private set; }
    public DateTime LastSyncedAt { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public void Update(string code, string name, string? address, DateTime now)
    {
        Code = code;
        Name = name;
        Address = address;
        LastSyncedAt = now;
        UpdatedAtUtc = now;
    }
}
