using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;
using PonPon.Modules.Payment.Application.Features.CreateCreditCardCharge;
using PonPon.Modules.Payment.Application.Features.CreateMobileBankingCharge;
using PonPon.Modules.Payment.Application.Features.CreatePromptPayCharge;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrderSubmitController : ControllerBase
{
    [HttpPost("submit")]
    public async Task<ActionResult<SubmitOrderResponse>> SubmitOrder(
        [FromBody] SubmitOrderRequest request,
        [FromServices] AddOrderHandler addOrder,
        [FromServices] CreatePromptPayChargeHandler promptPay,
        [FromServices] CreateMobileBankingChargeHandler mobileBanking,
        [FromServices] CreateCreditCardChargeHandler creditCard,
        CancellationToken cancellationToken)
    {
        var method = NormalizePaymentMethod(request.Payment?.Method ?? request.Order.PaymentMethod);
        var order = await addOrder.HandleAsync(
            ToAddOrderCommand(request.Order, method),
            cancellationToken);

        if (method is null)
            return Ok(new SubmitOrderResponse(order, null));

        var payment = method switch
        {
            "promptpay" => SubmitPaymentResponse.FromPromptPay(
                await promptPay.HandleAsync(
                    new CreatePromptPayChargeCommand(order.Id),
                    cancellationToken)),
            "mobile_banking" => SubmitPaymentResponse.FromMobileBanking(
                await mobileBanking.HandleAsync(
                    new CreateMobileBankingChargeCommand(
                        order.Id,
                        Required(request.Payment?.BankType, "bankType"),
                        Required(request.Payment?.ReturnUri, "returnUri")),
                    cancellationToken)),
            "card" => SubmitPaymentResponse.FromCreditCard(
                await creditCard.HandleAsync(
                    new CreateCreditCardChargeCommand(
                        order.Id,
                        Required(request.Payment?.TokenId, "tokenId"),
                        request.Payment?.ReturnUri),
                    cancellationToken)),
            _ => throw new BadRequestException($"Unsupported payment method: {request.Payment?.Method}")
        };

        return Ok(new SubmitOrderResponse(order, payment));
    }

    private static AddOrderCommand ToAddOrderCommand(AddOrderRequest request, string? paymentMethod)
        => new(
            request.ClientRequestId,
            request.QuoteId,
            request.CustomerName,
            request.CustomerEmail,
            request.CustomerPhone,
            request.CustomerAddress,
            request.ShippingName,
            request.ShippingPhone,
            request.ShippingAddress,
            request.ShippingChannel,
            request.ShippingAmount,
            request.CouponCode,
            request.Description,
            request.Items.Select(x => new AddOrderItemCommand(x.ProductId, x.VariantId, x.Quantity)).ToArray(),
            paymentMethod,
            request.CouponCodes);

    private static string? NormalizePaymentMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToLowerInvariant() switch
        {
            "promptpay" or "prompt_pay" or "prompt-pay" => "promptpay",
            "mobile_banking" or "mobile-banking" => "mobile_banking",
            "card" or "credit_card" or "credit-card" => "card",
            var method => method
        };
    }

    private static string Required(string? value, string fieldName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new BadRequestException($"{fieldName} is required.")
            : value.Trim();
}

public sealed record SubmitOrderRequest(
    AddOrderRequest Order,
    SubmitPaymentRequest? Payment = null);

public sealed record SubmitPaymentRequest(
    string Method,
    string? TokenId = null,
    string? BankType = null,
    string? ReturnUri = null);

public sealed record SubmitOrderResponse(
    AddOrderResponse Order,
    SubmitPaymentResponse? Payment);

public sealed record SubmitPaymentResponse(
    string Method,
    string ChargeId,
    string Status,
    string? QrCodeUrl = null,
    string? AuthorizeUri = null,
    int? Amount = null,
    string? Currency = null,
    DateTime? ExpiresAt = null)
{
    public static SubmitPaymentResponse FromPromptPay(CreatePromptPayChargeResponse response)
        => new(
            "promptpay",
            response.ChargeId,
            "pending",
            QrCodeUrl: response.QrCodeUrl,
            Amount: response.Amount,
            Currency: response.Currency,
            ExpiresAt: response.ExpiresAt);

    public static SubmitPaymentResponse FromMobileBanking(CreateMobileBankingChargeResponse response)
        => new(
            "mobile_banking",
            response.ChargeId,
            response.Status,
            AuthorizeUri: response.AuthorizeUri);

    public static SubmitPaymentResponse FromCreditCard(CreateCreditCardChargeResponse response)
        => new(
            "card",
            response.ChargeId,
            response.Status,
            AuthorizeUri: response.AuthorizeUri);
}
