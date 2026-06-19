using PonPon.Shared.Domain;

namespace PonPon.Modules.Identity.Domain.Customers;

public sealed class Customer : AggregateRoot, IAuditableEntity
{
    private Customer()
    {
        LineProfile = new LineProfile(string.Empty, string.Empty, null, null);
    }

    private Customer(LineProfile lineProfile, DateTime nowUtc)
    {
        LineProfile = lineProfile;
        Status = CustomerStatus.Active;
        CreatedAtUtc = nowUtc;
        LastLoginAtUtc = nowUtc;
    }

    public LineProfile LineProfile { get; private set; }
    public CustomerStatus Status { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public static Customer Create(LineProfile lineProfile, DateTime nowUtc) => new(lineProfile, nowUtc);

    public void UpdateLineProfile(LineProfile lineProfile, DateTime nowUtc)
    {
        LineProfile = lineProfile;
        LastLoginAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }
}
