namespace PonPon.Modules.Identity.Application.Features.CustomerAddresses;

public sealed record CreateCustomerAddressCommand(
    string RecipientName,
    string Phone,
    string? Email,
    string AddressLine1,
    string? AddressLine2,
    string Subdistrict,
    string District,
    string Province,
    string Postcode,
    string? Country,
    string? Label,
    bool IsDefault);
