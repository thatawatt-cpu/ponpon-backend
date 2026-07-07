namespace PonPon.Modules.Payment.Application.Features.CreateCreditCardCharge;

public sealed record CreateCreditCardChargeCommand(Guid OrderId, string TokenId, string? ReturnUri);

public sealed record CreateCreditCardChargeRequest(Guid OrderId, string TokenId, string? ReturnUri);

public sealed record CreateCreditCardChargeResponse(
    string ChargeId,
    string Status,
    string? AuthorizeUri);
