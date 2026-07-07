using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;

namespace PonPon.Modules.Shipping.Application.Abstractions;

public interface IShippopClient
{
    Task<IReadOnlyList<ShippopRateDto>> CheckRatesAsync(
        ShippopAddress to,
        ShippopParcel parcel,
        CancellationToken cancellationToken = default);

    Task<ShippopBookingDto> CreateBookingAsync(
        ShippopAddress to,
        ShippopParcel parcel,
        string courierCode,
        string serviceCode,
        string? remark,
        decimal cod,
        CancellationToken cancellationToken = default);

    Task<ShippopBookingDetailDto> GetBookingAsync(
        string trackingCode,
        CancellationToken cancellationToken = default);

    Task CancelBookingAsync(
        string trackingCode,
        CancellationToken cancellationToken = default);
}
