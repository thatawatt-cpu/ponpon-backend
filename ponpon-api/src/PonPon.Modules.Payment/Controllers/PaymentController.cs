using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PonPon.Modules.Payment.Application.Features.CreateCreditCardCharge;
using PonPon.Modules.Payment.Application.Features.CreateMobileBankingCharge;
using PonPon.Modules.Payment.Application.Features.CreatePromptPayCharge;
using PonPon.Modules.Payment.Application.Features.GetChargeStatus;
using PonPon.Modules.Payment.Infrastructure.ExternalServices.Omise;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace PonPon.Modules.Payment.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentController : ControllerBase
{
    [HttpGet("omise-config")]
    [AllowAnonymous]
    public async Task<ActionResult<OmiseConfigResponse>> GetOmiseConfig(
        [FromServices] IRuntimeSettingProvider settings,
        [FromServices] IOptions<OmiseOptions> options,
        CancellationToken cancellationToken)
    {
        var publicKey = await settings.GetValueAsync("Omise", "PublicKey", cancellationToken)
            ?? options.Value.PublicKey;
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new BadRequestException("Omise public key is not configured.");

        Response.Headers["Cache-Control"] = "public, max-age=86400, s-maxage=86400, stale-while-revalidate=3600";
        var etag = OmiseConfigCache.CreateWeakEtag(publicKey.Trim());
        Response.Headers["ETag"] = etag;
        if (Request.Headers.IfNoneMatch.Any(x => string.Equals(x, etag, StringComparison.Ordinal)))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(new OmiseConfigResponse(publicKey));
    }

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

public sealed record OmiseConfigResponse(string PublicKey);

file static class OmiseConfigCache
{
    public static string CreateWeakEtag(string value)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        return $"W/\"{hash}\"";
    }
}
