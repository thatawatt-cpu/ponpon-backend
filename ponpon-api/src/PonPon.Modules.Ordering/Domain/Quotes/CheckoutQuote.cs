namespace PonPon.Modules.Ordering.Domain.Quotes;

public sealed class CheckoutQuote
{
    private CheckoutQuote()
    {
        PayloadHash = string.Empty;
        CalculationHash = string.Empty;
        CalculationStatus = string.Empty;
        PricingSnapshotJson = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string PayloadHash { get; private set; }
    public string CalculationHash { get; private set; }
    public string PricingSnapshotJson { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsFinal { get; private set; }
    public string CalculationStatus { get; private set; }
    public bool ShippingFinalized { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public Guid? ClientRequestId { get; private set; }

    public static CheckoutQuote Create(
        Guid customerId,
        string payloadHash,
        string calculationHash,
        string pricingSnapshotJson,
        DateTime expiresAtUtc,
        bool isFinal,
        string calculationStatus,
        bool shippingFinalized,
        DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PayloadHash = payloadHash,
            CalculationHash = calculationHash,
            PricingSnapshotJson = pricingSnapshotJson,
            ExpiresAtUtc = expiresAtUtc,
            IsFinal = isFinal,
            CalculationStatus = calculationStatus,
            ShippingFinalized = shippingFinalized,
            CreatedAtUtc = now
        };

    public void MarkUsed(Guid clientRequestId, DateTime now)
    {
        ClientRequestId = clientRequestId;
        UsedAtUtc = now;
    }
}
