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
    DateTime? ReceivedAtUtc,
    string? Reference,
    string? Description,
    bool IsCod,
    string? Currency,
    string? CancellationReason,
    DateTime? CanceledAtUtc,
    string? OmiseRefundStatus,
    decimal RefundedAmount,
    string? PricingSnapshotJson,
    IReadOnlyCollection<MyOrderItemResponse> Items,
    IReadOnlyCollection<MyOrderPaymentResponse> Payments);

public sealed record MyOrderItemOptionResponse(string Name, string Value);

public sealed record MyOrderItemResponse(
    Guid Id,
    Guid? ProductId,
    Guid? VariantId,
    string Sku,
    string Name,
    decimal Quantity,
    string? UnitText,
    decimal PricePerUnit,
    string? Discount,
    decimal DiscountAmount,
    decimal TotalPrice,
    string? ImageUrl,
    Guid? ReviewId,
    bool IsReviewed,
    IReadOnlyCollection<MyOrderItemOptionResponse> Options);

public sealed record MyOrderPaymentResponse(
    Guid Id,
    string Name,
    decimal Amount,
    DateTime? PaymentDateTime);
