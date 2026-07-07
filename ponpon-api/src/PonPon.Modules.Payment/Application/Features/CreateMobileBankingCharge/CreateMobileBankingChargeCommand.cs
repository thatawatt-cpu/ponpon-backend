namespace PonPon.Modules.Payment.Application.Features.CreateMobileBankingCharge;

public sealed record CreateMobileBankingChargeCommand(Guid OrderId, string BankType, string ReturnUri);

public sealed record CreateMobileBankingChargeRequest(Guid OrderId, string BankType, string ReturnUri);

public sealed record CreateMobileBankingChargeResponse(
    string ChargeId,
    string Status,
    string AuthorizeUri);
