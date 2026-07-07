namespace PonPon.Modules.Shipping.Application.Features.ShippopSender;

public sealed record UpdateShippopSenderRequest(
    string Name,
    string Phone,
    string Email,
    string Address,
    string District,
    string State,
    string Province,
    string Postcode);

public sealed record UpdateShippopSenderCommand(
    string Name,
    string Phone,
    string Email,
    string Address,
    string District,
    string State,
    string Province,
    string Postcode);
