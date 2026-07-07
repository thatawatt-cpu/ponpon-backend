namespace PonPon.Modules.Shipping.Application.Features.ShippopSender;

using SenderEntity = PonPon.Modules.Shipping.Domain.ShippopSender;

public sealed record ShippopSenderResponse(
    string Name,
    string Phone,
    string Email,
    string Address,
    string District,
    string State,
    string Province,
    string Postcode,
    bool IsConfigured,
    DateTime? UpdatedAtUtc)
{
    public static ShippopSenderResponse Empty() =>
        new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty, false, null);

    public static ShippopSenderResponse From(SenderEntity sender) =>
        new(
            sender.Name,
            sender.Phone,
            sender.Email,
            sender.Address,
            sender.District,
            sender.State,
            sender.Province,
            sender.Postcode,
            true,
            sender.UpdatedAtUtc);
}
