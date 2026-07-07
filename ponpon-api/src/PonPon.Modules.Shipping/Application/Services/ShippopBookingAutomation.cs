using System.Text.Json;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Domain;
using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Shipping.Application.Services;

public sealed class ShippopBookingAutomation : IShippingBookingAutomation
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IShippopClient _shippop;
    private readonly IShippingShipmentRepository _shipments;
    private readonly IShippingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ShippopBookingAutomation(
        IShippopClient shippop,
        IShippingShipmentRepository shipments,
        IShippingUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _shippop = shippop;
        _shipments = shipments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ShippingBookingResult?> CreateBookingForPackedOrderAsync(
        ShippingBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var existing = await _shipments.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            return new ShippingBookingResult(
                existing.ShippopTrackingCode,
                existing.CourierTrackingCode,
                existing.CourierCode ?? string.Empty);
        }

        var to = new ShippopAddress(
            request.ToName,
            request.ToAddress,
            request.ToDistrict,
            request.ToState,
            request.ToProvince,
            request.ToPostcode,
            request.ToPhone,
            request.ToEmail ?? string.Empty);

        var parcel = new ShippopParcel(
            request.ParcelName,
            request.WeightKg,
            request.WidthCm,
            request.LengthCm,
            request.HeightCm);

        var booking = await _shippop.CreateBookingAsync(
            to,
            parcel,
            request.CourierCode,
            request.CourierCode,
            request.Remark,
            request.Cod,
            cancellationToken);

        var now = _clock.UtcNow;
        var (shipment, _) = await _shipments.GetOrCreateByShippopTrackingCodeAsync(
            booking.TrackingCode,
            "booking",
            now,
            cancellationToken);

        shipment.UpdateFromBooking(
            request.OrderId,
            request.OrderNumber,
            booking.CourierTrackingCode,
            booking.CourierCode,
            courierName: null,
            booking.Price,
            booking.LabelUrl,
            booking.RawJson,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ShippingBookingResult(
            booking.TrackingCode,
            booking.CourierTrackingCode,
            booking.CourierCode);
    }

    public async Task CancelBookingForOrderAsync(
        Guid orderId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _shipments.GetByOrderIdAsync(orderId, cancellationToken);
        if (shipment is null)
        {
            return;
        }

        await _shippop.CancelBookingAsync(shipment.ShippopTrackingCode, cancellationToken);

        var now = _clock.UtcNow;
        var payloadJson = JsonSerializer.Serialize(new
        {
            reason,
            canceledAtUtc = now
        }, JsonOptions);

        shipment.MarkCanceled(payloadJson, now);
        shipment.AddEvent(ShippingShipmentEvent.Create(
            shipment.Id,
            shipment.ShippopTrackingCode,
            "canceled",
            shipment.CourierTrackingCode,
            now,
            payloadJson,
            now));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasShipmentForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _shipments.GetByOrderIdAsync(orderId, cancellationToken) is not null;
    }

    private static void Validate(ShippingBookingRequest request)
    {
        if (request.OrderId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.OrderNumber)
            || string.IsNullOrWhiteSpace(request.ToName)
            || string.IsNullOrWhiteSpace(request.ToPhone)
            || string.IsNullOrWhiteSpace(request.ToAddress)
            || string.IsNullOrWhiteSpace(request.ToDistrict)
            || string.IsNullOrWhiteSpace(request.ToState)
            || string.IsNullOrWhiteSpace(request.ToProvince)
            || string.IsNullOrWhiteSpace(request.ToPostcode)
            || string.IsNullOrWhiteSpace(request.ParcelName)
            || string.IsNullOrWhiteSpace(request.CourierCode))
        {
            throw new BadRequestException("Packed order is missing shipping booking information.");
        }

        if (request.WeightKg <= 0 || request.WidthCm <= 0 || request.LengthCm <= 0 || request.HeightCm <= 0)
        {
            throw new BadRequestException("Packed order parcel dimensions are required.");
        }
    }
}
