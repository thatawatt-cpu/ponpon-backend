using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Payment.Application.Features.CreateCreditCardCharge;
using PonPon.Modules.Payment.Application.Features.CreateMobileBankingCharge;
using PonPon.Modules.Payment.Application.Features.CreatePromptPayCharge;
using PonPon.Modules.Payment.Application.Features.GetChargeStatus;

namespace PonPon.Modules.Payment.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentController : ControllerBase
{
    [HttpPost("promptpay")]
    public async Task<ActionResult<CreatePromptPayChargeResponse>> CreatePromptPayCharge(
        [FromBody] CreatePromptPayChargeRequest request,
        [FromServices] CreatePromptPayChargeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreatePromptPayChargeCommand(request.OrderId),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("mobile-banking")]
    public async Task<ActionResult<CreateMobileBankingChargeResponse>> CreateMobileBankingCharge(
        [FromBody] CreateMobileBankingChargeRequest request,
        [FromServices] CreateMobileBankingChargeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateMobileBankingChargeCommand(request.OrderId, request.BankType, request.ReturnUri),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("credit-card")]
    public async Task<ActionResult<CreateCreditCardChargeResponse>> CreateCreditCardCharge(
        [FromBody] CreateCreditCardChargeRequest request,
        [FromServices] CreateCreditCardChargeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateCreditCardChargeCommand(request.OrderId, request.TokenId, request.ReturnUri),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{chargeId}/status")]
    public async Task<ActionResult<GetChargeStatusResponse>> GetChargeStatus(
        string chargeId,
        [FromServices] GetChargeStatusHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetChargeStatusQuery(chargeId), cancellationToken));
    }
}
