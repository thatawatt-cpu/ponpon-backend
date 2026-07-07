namespace PonPon.Modules.Payment.Application.Features.CreatePromptPayCharge;

public sealed record CreatePromptPayChargeCommand(Guid OrderId);

public sealed record CreatePromptPayChargeRequest(Guid OrderId);

public sealed record CreatePromptPayChargeResponse(
    string ChargeId,
    string QrCodeUrl,
    int Amount,
    string Currency,
    DateTime? ExpiresAt);
