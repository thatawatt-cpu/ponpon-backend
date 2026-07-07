using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.PreviewPricing;
using PonPon.Modules.Ordering.Application.Features.Orders.CancelMyOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;
using PonPon.Modules.Ordering.Application.Features.Orders.ReturnOrder;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrderController : ControllerBase
{
    [HttpPost("pricing-preview")]
    public async Task<ActionResult<PreviewPricingResponse>> PreviewPricing(
        [FromBody] PreviewPricingRequest request,
        [FromServices] PreviewPricingHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(request, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AddOrderResponse>> AddOrder(
        [FromBody] AddOrderRequest request,
        [FromServices] AddOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new AddOrderCommand(
            request.ClientRequestId,
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
            request.PaymentMethod,
            request.CouponCodes);

        return Ok(await handler.HandleAsync(command, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<MyOrdersPagedResponse>> GetMyOrders(
        [FromQuery] GetMyOrdersRequest request,
        [FromQuery(Name = "status")] string[]? statuses,
        [FromQuery(Name = "paymentstatus")] string[]? paymentStatuses,
        [FromServices] GetMyOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        var statusFilter = statuses is { Length: > 0 }
            ? string.Join(",", statuses)
            : request.Status;
        var paymentStatusFilter = paymentStatuses is { Length: > 0 }
            ? string.Join(",", paymentStatuses)
            : request.PaymentStatus;

        return Ok(await handler.HandleAsync(
            new GetMyOrdersQuery(statusFilter, paymentStatusFilter, request.Page, request.PageSize),
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MyOrderDetailResponse>> GetMyOrderById(
        Guid id,
        [FromServices] GetMyOrderByIdHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetMyOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelMyOrder(
        Guid id,
        [FromBody] CancelMyOrderRequest request,
        [FromServices] CancelMyOrderHandler handler,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated
            || currentUser.UserType != "Customer"
            || currentUser.CustomerId is not Guid customerId)
        {
            return Forbid();
        }

        await handler.HandleAsync(new CancelMyOrderCommand(id, customerId, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/return-request")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<OrderReturnRequestResponse>> CreateReturnRequest(
        Guid id,
        [FromForm] string reason,
        [FromForm] List<IFormFile> photos,
        [FromServices] CreateOrderReturnRequestHandler handler,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated
            || currentUser.UserType != "Customer"
            || currentUser.CustomerId is not Guid customerId)
        {
            return Forbid();
        }

        var streams = photos.Select(x => x.OpenReadStream()).ToArray();
        try
        {
            var files = photos.Select((file, index) => new ReturnEvidenceFile(
                streams[index],
                file.FileName,
                file.ContentType,
                file.Length)).ToArray();
            var response = await handler.HandleAsync(
                new CreateOrderReturnRequestCommand(id, customerId, reason, files),
                cancellationToken);
            return Ok(response);
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }
    }

    [HttpGet("{id:guid}/return-request")]
    public async Task<ActionResult<OrderReturnRequestResponse>> GetMyReturnRequest(
        Guid id,
        [FromServices] GetMyOrderReturnRequestHandler handler,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated
            || currentUser.UserType != "Customer"
            || currentUser.CustomerId is not Guid customerId)
        {
            return Forbid();
        }

        return Ok(await handler.HandleAsync(
            new GetMyOrderReturnRequestQuery(id, customerId),
            cancellationToken));
    }
}
