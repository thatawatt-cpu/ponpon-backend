using PonPon.Shared.Domain;

namespace PonPon.Modules.Ordering.Domain.Orders;

public sealed class Order : AggregateRoot, IAuditableEntity
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderPayment> _payments = [];

    private Order()
    {
        Number = string.Empty;
        Status = string.Empty;
        PaymentStatus = string.Empty;
        SalesChannel = string.Empty;
    }

    private Order(DateTime now) : this()
    {
        CreatedAtUtc = now;
    }

    public long ZortOrderId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string Number { get; private set; }
    public long? ZortCustomerId { get; private set; }
    public string? CustomerCode { get; private set; }
    public string? CustomerName { get; private set; }
    public string? CustomerIdNumber { get; private set; }
    public string? CustomerEmail { get; private set; }
    public string? CustomerPhone { get; private set; }
    public string? CustomerAddress { get; private set; }
    public string Status { get; private set; }
    public string PaymentStatus { get; private set; }
    public decimal Amount { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal ShippingAmount { get; private set; }
    public decimal PaymentAmount { get; private set; }
    public string? OmiseChargeId { get; private set; }
    public string? CheckoutPaymentMethod { get; private set; }
    public string? OmiseRefundId { get; private set; }
    public string? OmiseRefundStatus { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public DateTime? OmiseRefundedAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CanceledBy { get; private set; }
    public DateTime? CanceledAtUtc { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public string? ShippingChannel { get; private set; }
    public string? ShippingName { get; private set; }
    public string? ShippingAddress { get; private set; }
    public string? ShippingPhone { get; private set; }
    public string? TrackingNo { get; private set; }
    public DateTime? OrderDate { get; private set; }
    public DateTime? ShippingDate { get; private set; }
    public string? Reference { get; private set; }
    public string? Description { get; private set; }
    public string SalesChannel { get; private set; }
    public string? IntegrationCustomerId { get; private set; }
    public string? IntegrationCustomer { get; private set; }
    public string? WarehouseCode { get; private set; }
    public bool IsCod { get; private set; }
    public string? Currency { get; private set; }
    public string? TagsJson { get; private set; }
    public DateTime? PaymentExpiresAt { get; private set; }
    public bool HasStockReservation { get; private set; }
    public bool IsPaymentCreationPending { get; private set; }
    public DateTime? PaymentCreationStartedAtUtc { get; private set; }
    public DateTime? ReceivedAtUtc { get; private set; }
    public DateTime? DeliveredNotificationSentAtUtc { get; private set; }
    public DateTime? ZortCreatedAt { get; private set; }
    public DateTime? ZortUpdatedAt { get; private set; }
    public string RawZortJson { get; private set; } = "{}";
    public string? PricingSnapshotJson { get; private set; }
    public DateTime LastSyncedAt { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderPayment> Payments => _payments.AsReadOnly();

    public static Order CreateFromZort(OrderSnapshot snapshot, DateTime now)
    {
        var order = new Order(now);
        order.ApplyZortSnapshot(snapshot, now);
        order._items.AddRange(snapshot.Items.Select(x => OrderItem.FromSnapshot(order.Id, x)));
        order._payments.AddRange(snapshot.Payments.Select(x => OrderPayment.FromSnapshot(order.Id, x)));
        return order;
    }

    public void AssignCustomer(Guid customerId, DateTime now)
    {
        CustomerId = customerId;
        UpdatedAtUtc = now;
    }

    public void SetPaymentExpiry(DateTime expiresAt, DateTime now)
    {
        PaymentExpiresAt = expiresAt;
        UpdatedAtUtc = now;
    }

    public void MarkStockReserved(DateTime now)
    {
        HasStockReservation = true;
        UpdatedAtUtc = now;
    }

    public void MarkStockReleased(DateTime now)
    {
        HasStockReservation = false;
        UpdatedAtUtc = now;
    }

    public void BeginPaymentCreation(DateTime now)
    {
        IsPaymentCreationPending = true;
        PaymentCreationStartedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void CompletePaymentCreation(DateTime now)
    {
        IsPaymentCreationPending = false;
        PaymentCreationStartedAtUtc = null;
        UpdatedAtUtc = now;
    }

    public void SetPricingSnapshot(string pricingSnapshotJson, DateTime now)
    {
        PricingSnapshotJson = string.IsNullOrWhiteSpace(pricingSnapshotJson)
            ? throw new ArgumentException("Pricing snapshot is required.", nameof(pricingSnapshotJson))
            : pricingSnapshotJson;
        UpdatedAtUtc = now;
    }

    public void SetCheckoutPaymentMethod(string? paymentMethod, DateTime now)
    {
        CheckoutPaymentMethod = string.IsNullOrWhiteSpace(paymentMethod)
            ? null
            : paymentMethod.Trim().ToLowerInvariant();
        UpdatedAtUtc = now;
    }

    public void ApplyZortSnapshot(OrderSnapshot snapshot, DateTime now)
    {
        ZortOrderId = snapshot.ZortOrderId;
        Number = snapshot.Number;
        ZortCustomerId = snapshot.ZortCustomerId;
        CustomerCode = snapshot.CustomerCode;
        CustomerName = snapshot.CustomerName;
        CustomerIdNumber = snapshot.CustomerIdNumber;
        CustomerEmail = snapshot.CustomerEmail;
        CustomerPhone = snapshot.CustomerPhone;
        CustomerAddress = snapshot.CustomerAddress;
        Status = snapshot.Status;
        PaymentStatus = snapshot.PaymentStatus;
        Amount = snapshot.Amount;
        VatAmount = snapshot.VatAmount;
        ShippingAmount = snapshot.ShippingAmount;
        PaymentAmount = snapshot.PaymentAmount;
        DiscountAmount = snapshot.DiscountAmount;
        ShippingChannel = snapshot.ShippingChannel;
        ShippingName = snapshot.ShippingName;
        ShippingAddress = snapshot.ShippingAddress;
        ShippingPhone = snapshot.ShippingPhone;
        TrackingNo = snapshot.TrackingNo;
        OrderDate = snapshot.OrderDate;
        ShippingDate = snapshot.ShippingDate;
        Reference = snapshot.Reference;
        Description = snapshot.Description;
        SalesChannel = snapshot.SalesChannel;
        IntegrationCustomerId = string.IsNullOrWhiteSpace(snapshot.IntegrationCustomerId)
            ? IntegrationCustomerId
            : snapshot.IntegrationCustomerId;
        IntegrationCustomer = string.IsNullOrWhiteSpace(snapshot.IntegrationCustomer)
            ? IntegrationCustomer
            : snapshot.IntegrationCustomer;
        WarehouseCode = snapshot.WarehouseCode;
        IsCod = snapshot.IsCod;
        Currency = snapshot.Currency;
        TagsJson = snapshot.TagsJson;
        ZortCreatedAt = snapshot.ZortCreatedAt;
        ZortUpdatedAt = snapshot.ZortUpdatedAt;
        RawZortJson = snapshot.RawZortJson;
        LastSyncedAt = now;
        UpdatedAtUtc = now;
    }

    public void MarkVoided(
        DateTime now,
        string? cancellationReason = null,
        string? canceledBy = null)
    {
        Status = "Voided";
        PaymentStatus = "Voided";
        PaymentExpiresAt = null;
        CancellationReason = string.IsNullOrWhiteSpace(cancellationReason)
            ? CancellationReason
            : cancellationReason.Trim();
        CanceledBy = string.IsNullOrWhiteSpace(canceledBy)
            ? CanceledBy
            : canceledBy.Trim();
        CanceledAtUtc = now;
        LastSyncedAt = now;
        UpdatedAtUtc = now;
    }

    public void RegisterOmiseCharge(string chargeId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            throw new ArgumentException("Omise charge id is required.", nameof(chargeId));
        }

        OmiseChargeId = chargeId.Trim();
        UpdatedAtUtc = now;
    }

    public void RequestManualRefund(
        string reason,
        string requestedBy,
        DateTime now)
    {
        OmiseRefundStatus = OrderRefundStatus.ManualRefundPending;
        CancellationReason = reason.Trim();
        CanceledBy = requestedBy.Trim();
        UpdatedAtUtc = now;
    }

    public void RecordOmiseRefund(
        string? refundId,
        string status,
        decimal refundedAmount,
        DateTime now)
    {
        OmiseRefundId = string.IsNullOrWhiteSpace(refundId) ? OmiseRefundId : refundId.Trim();
        OmiseRefundStatus = string.IsNullOrWhiteSpace(status) ? "closed" : status.Trim();
        RefundedAmount = refundedAmount;
        OmiseRefundedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void ApplyShippingStatus(string status, string? trackingNumber, DateTime now)
    {
        Status = status;
        TrackingNo = string.IsNullOrWhiteSpace(trackingNumber)
            ? TrackingNo
            : trackingNumber.Trim();
        LastSyncedAt = now;
        UpdatedAtUtc = now;
    }

    public void MarkReceived(DateTime now)
    {
        ReceivedAtUtc ??= now;
        UpdatedAtUtc = now;
    }

    public void MarkDeliveredNotificationSent(DateTime now)
    {
        DeliveredNotificationSentAtUtc ??= now;
        UpdatedAtUtc = now;
    }
}

public sealed record OrderSnapshot(
    long ZortOrderId,
    string Number,
    long? ZortCustomerId,
    string? CustomerCode,
    string? CustomerName,
    string? CustomerIdNumber,
    string? CustomerEmail,
    string? CustomerPhone,
    string? CustomerAddress,
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
    string SalesChannel,
    string? IntegrationCustomerId,
    string? IntegrationCustomer,
    string? WarehouseCode,
    bool IsCod,
    string? Currency,
    string? TagsJson,
    DateTime? ZortCreatedAt,
    DateTime? ZortUpdatedAt,
    IReadOnlyCollection<OrderItemSnapshot> Items,
    IReadOnlyCollection<OrderPaymentSnapshot> Payments,
    string RawZortJson);
