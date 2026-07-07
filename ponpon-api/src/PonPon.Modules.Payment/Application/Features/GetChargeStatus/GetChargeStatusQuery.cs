namespace PonPon.Modules.Payment.Application.Features.GetChargeStatus;

public sealed record GetChargeStatusQuery(string ChargeId);

public sealed record GetChargeStatusResponse(
    string ChargeId,
    string Status,
    bool Paid,
    DateTime? PaidAt,
    string? FailureCode,
    string? FailureMessage);
