using PonPon.Shared.Domain;

namespace PonPon.Modules.Identity.Domain.Customers;

public sealed class Customer : AggregateRoot, IAuditableEntity
{
    private readonly List<CustomerAddress> _addresses = [];

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
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();
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

    public CustomerAddress AddAddress(
        string recipientName,
        string phone,
        string? email,
        string addressLine1,
        string? addressLine2,
        string subdistrict,
        string district,
        string province,
        string postcode,
        string country,
        string? label,
        bool isDefault,
        DateTime nowUtc)
    {
        if (isDefault)
        {
            foreach (var address in _addresses)
            {
                address.SetDefault(false, nowUtc);
            }
        }

        var customerAddress = CustomerAddress.Create(
            Id,
            recipientName,
            phone,
            email,
            addressLine1,
            addressLine2,
            subdistrict,
            district,
            province,
            postcode,
            country,
            label,
            isDefault,
            nowUtc);

        _addresses.Add(customerAddress);
        UpdatedAtUtc = nowUtc;
        return customerAddress;
    }
}
