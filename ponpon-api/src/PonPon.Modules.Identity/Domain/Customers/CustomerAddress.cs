using PonPon.Shared.Domain;

namespace PonPon.Modules.Identity.Domain.Customers;

public sealed class CustomerAddress : Entity, IAuditableEntity
{
    private CustomerAddress()
    {
        Customer = null!;
        RecipientName = string.Empty;
        Phone = string.Empty;
        AddressLine1 = string.Empty;
        Subdistrict = string.Empty;
        District = string.Empty;
        Province = string.Empty;
        Postcode = string.Empty;
        Country = "TH";
    }

    private CustomerAddress(
        Guid customerId,
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
        CustomerId = customerId;
        Customer = null!;
        RecipientName = recipientName;
        Phone = phone;
        Email = email;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        Subdistrict = subdistrict;
        District = district;
        Province = province;
        Postcode = postcode;
        Country = country;
        Label = label;
        IsDefault = isDefault;
        CreatedAtUtc = nowUtc;
    }

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; }
    public string RecipientName { get; private set; }
    public string Phone { get; private set; }
    public string? Email { get; private set; }
    public string AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string Subdistrict { get; private set; }
    public string District { get; private set; }
    public string Province { get; private set; }
    public string Postcode { get; private set; }
    public string Country { get; private set; }
    public string? Label { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public static CustomerAddress Create(
        Guid customerId,
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
        DateTime nowUtc) =>
        new(
            customerId,
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

    public void Update(
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
        DateTime nowUtc)
    {
        RecipientName = recipientName;
        Phone = phone;
        Email = email;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        Subdistrict = subdistrict;
        District = district;
        Province = province;
        Postcode = postcode;
        Country = country;
        Label = label;
        UpdatedAtUtc = nowUtc;
    }

    public void SetDefault(bool isDefault, DateTime nowUtc)
    {
        IsDefault = isDefault;
        UpdatedAtUtc = nowUtc;
    }

    public void Delete(DateTime nowUtc)
    {
        IsDeleted = true;
        IsDefault = false;
        UpdatedAtUtc = nowUtc;
    }

    public string ToFullAddress()
    {
        var addressParts = new[]
        {
            AddressLine1,
            AddressLine2,
            Subdistrict,
            District,
            Province,
            Postcode
        };

        return string.Join(" ", addressParts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
