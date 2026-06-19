namespace PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;

public sealed record MyOrderDetailResponse(
    Guid Id,
    string Number,
    string Status,
    string PaymentStatus,
    decimal Amount,
    decimal VatAmount,
    decimal ShippingAmount,
    decimal PaymentAmount,
    decimal DiscountAmount,
    string? ShippingChannel,
    string? ShippingName,
    string? ShippingAddress,
    string? ShippingPhone,
    string? TrackingNo,
    DateTime? OrderDate,
    DateTime? ShippingDate,
    string? Reference,
    string? Description,
    bool IsCod,
    string? Currency,
    IReadOnlyCollection<MyOrderItemResponse> Items,
    IReadOnlyCollection<MyOrderPaymentResponse> Payments);

public sealed record MyOrderItemResponse(
    Guid Id,
    string Sku,
    string Name,
    decimal Quantity,
    string? UnitText,
    decimal PricePerUnit,
    string? Discount,
    decimal DiscountAmount,
    decimal TotalPrice);

public sealed record MyOrderPaymentResponse(
    Guid Id,
    string Name,
    decimal Amount,
    DateTime? PaymentDateTime);
