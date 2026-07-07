using PonPon.Shared.Domain;

namespace PonPon.Modules.Shipping.Domain;

public sealed class ShippopSender : Entity, IAuditableEntity
{
    public static readonly Guid DefaultId = Guid.Parse("8a365ad3-9d0e-4a5d-b4aa-55a4b7dc438c");

    private ShippopSender()
    {
        Name = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        Address = string.Empty;
        District = string.Empty;
        State = string.Empty;
        Province = string.Empty;
        Postcode = string.Empty;
    }

    public string Name { get; private set; }
    public string Phone { get; private set; }
    public string Email { get; private set; }
    public string Address { get; private set; }
    public string District { get; private set; }
    public string State { get; private set; }
    public string Province { get; private set; }
    public string Postcode { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public static ShippopSender Create(
        string name,
        string phone,
        string email,
        string address,
        string district,
        string state,
        string province,
        string postcode,
        DateTime nowUtc)
    {
        var sender = new ShippopSender
        {
            Id = DefaultId,
            CreatedAtUtc = nowUtc
        };

        sender.Update(name, phone, email, address, district, state, province, postcode, nowUtc);
        return sender;
    }

    public void Update(
        string name,
        string phone,
        string email,
        string address,
        string district,
        string state,
        string province,
        string postcode,
        DateTime nowUtc)
    {
        Name = name;
        Phone = phone;
        Email = email;
        Address = address;
        District = district;
        State = state;
        Province = province;
        Postcode = postcode;
        UpdatedAtUtc = nowUtc;
    }
}
