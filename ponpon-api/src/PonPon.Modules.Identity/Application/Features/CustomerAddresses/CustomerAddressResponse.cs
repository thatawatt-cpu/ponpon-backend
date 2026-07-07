using PonPon.Modules.Identity.Domain.Customers;

namespace PonPon.Modules.Identity.Application.Features.CustomerAddresses;

public sealed record CustomerAddressResponse(
    Guid Id,
    string RecipientName,
    string Phone,
    string? Email,
    string AddressLine1,
    string? AddressLine2,
    string Subdistrict,
    string District,
    string Province,
    string Postcode,
    string Country,
    string? Label,
    bool IsDefault,
    string FullAddress);

public static class CustomerAddressResponseMapper
{
    public static CustomerAddressResponse ToResponse(this CustomerAddress address) =>
        new(
            address.Id,
            address.RecipientName,
            address.Phone,
            address.Email,
            address.AddressLine1,
            address.AddressLine2,
            address.Subdistrict,
            address.District,
            address.Province,
            address.Postcode,
            address.Country,
            address.Label,
            address.IsDefault,
            address.ToFullAddress());
}
