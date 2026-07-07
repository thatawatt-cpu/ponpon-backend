namespace PonPon.Modules.Payment.Application.Features.HandleOmiseWebhook;

public sealed record HandleOmiseWebhookCommand(
    string EventKey,
    string ChargeId,
    string ChargeStatus,
    bool Paid,
    DateTime? PaidAt,
    int AmountSatang,
    string? Description,
    string? SourceType);
