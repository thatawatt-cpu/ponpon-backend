namespace PonPon.Modules.Payment.Application.Abstractions;

public interface IOmiseClient
{
    Task<OmiseChargeResult> CreatePromptPayChargeAsync(int amount, string currency, string? description, CancellationToken cancellationToken);
    Task<OmiseChargeResult> CreateMobileBankingChargeAsync(string bankType, int amount, string currency, string? description, string returnUri, CancellationToken cancellationToken);
    Task<OmiseChargeResult> CreateCreditCardChargeAsync(string tokenId, int amount, string currency, string? description, string? returnUri, CancellationToken cancellationToken);
    Task<OmiseChargeResult> GetChargeAsync(string chargeId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OmiseChargeResult>> FindChargesByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OmiseChargeResult>> FindSuccessfulChargesByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken);
    Task<OmiseRefundResult> CreateRefundAsync(
        string chargeId,
        int amount,
        string orderNumber,
        CancellationToken cancellationToken);
}

public sealed record OmiseChargeResult(
    string ChargeId,
    string Status,
    bool Paid,
    DateTime? PaidAt,
    string? AuthorizeUri,
    string? QrCodeUrl,
    string? FailureCode,
    string? FailureMessage,
    DateTime? ExpiresAt,
    int Amount,
    string Currency,
    bool Refundable,
    int RefundedAmount,
    bool Voided,
    string? SourceType,
    string? Description);

public sealed record OmiseRefundResult(
    string RefundId,
    string ChargeId,
    string Status,
    int Amount,
    string Currency,
    bool Voided,
    DateTime? CreatedAt);
