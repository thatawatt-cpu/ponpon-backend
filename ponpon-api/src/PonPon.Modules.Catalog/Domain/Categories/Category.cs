using PonPon.Shared.Domain;

namespace PonPon.Modules.Catalog.Domain.Categories;

public sealed class Category : Entity, IAuditableEntity
{
    private Category()
    {
        Name = string.Empty;
    }

    public Category(long? zortCategoryId, string name, DateTime now)
    {
        ZortCategoryId = zortCategoryId;
        Name = name;
        IsActive = true;
        CreatedAtUtc = now;
    }

    public long? ZortCategoryId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get => CreatedAtUtc; private set => CreatedAtUtc = value; }
    public DateTime? UpdatedAt { get => UpdatedAtUtc; private set => UpdatedAtUtc = value; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public void Update(string name, bool isActive, DateTime now)
    {
        Name = name;
        IsActive = isActive;
        UpdatedAtUtc = now;
    }
}
