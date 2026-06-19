using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.CancelMyOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrderController : ControllerBase
{
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
            request.Description,
            request.Items.Select(x => new AddOrderItemCommand(x.ProductId, x.VariantId, x.Quantity)).ToArray());

        return Ok(await handler.HandleAsync(command, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MyOrderListItemResponse>>> GetMyOrders(
        [FromQuery] GetMyOrdersRequest request,
        [FromServices] GetMyOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new GetMyOrdersQuery(request.Status, request.PaymentStatus, request.Page, request.PageSize),
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

        await handler.HandleAsync(new CancelMyOrderCommand(id, customerId), cancellationToken);
        return NoContent();
    }
}
