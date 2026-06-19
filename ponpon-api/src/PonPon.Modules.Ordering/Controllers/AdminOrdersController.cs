using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Ordering.Application.Features.Orders.CancelOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.GetOrderById;
using PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;

namespace PonPon.Modules.Ordering.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public sealed class AdminOrdersController : ControllerBase
{
    [HttpPost("sync-zort")]
    public async Task<ActionResult<SyncOrdersFromZortResponse>> SyncZort(
        [FromBody] SyncOrdersFromZortRequest request,
        [FromServices] SyncOrdersFromZortHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new SyncOrdersFromZortCommand(request.PageStart, request.PageLimit, request.MaxPages),
            cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<OrderListItemResponse>>> GetOrders(
        [FromQuery] GetOrdersRequest request,
        [FromServices] GetOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new GetOrdersQuery(request.Keyword, request.Status, request.PaymentStatus, request.Page, request.PageSize),
            cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromServices] CancelOrderHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new CancelOrderCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailResponse>> GetOrderById(
        Guid id,
        [FromServices] GetOrderByIdHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken));
    }
}
