using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.ApproveManualRefund;
using PonPon.Modules.Ordering.Application.Features.Orders.CancelOrder;
using PonPon.Modules.Ordering.Application.Features.Orders.GetOrderById;
using PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;
using PonPon.Modules.Ordering.Application.Features.Orders.ReturnOrder;
using PonPon.Modules.Ordering.Domain.SyncRuns;
using PonPon.Modules.Ordering.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public sealed class AdminOrdersController : ControllerBase
{
    [HttpGet("{id:guid}/pricing-snapshot")]
    public async Task<ActionResult<JsonElement>> GetPricingSnapshot(
        Guid id,
        [FromServices] IOrderRepository orders,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound();
        if (string.IsNullOrWhiteSpace(order.PricingSnapshotJson))
            return NoContent();

        return Ok(JsonSerializer.Deserialize<JsonElement>(order.PricingSnapshotJson));
    }

    [HttpPost("sync-zort")]
    public async Task<ActionResult<OrderSyncQueuedResponse>> SyncZort(
        [FromBody] SyncOrdersFromZortRequest request,
        [FromServices] IOrderSyncRunRepository syncRuns,
        [FromServices] IOrderingUnitOfWork unitOfWork,
        [FromServices] IPersistentBackgroundJobClient backgroundJobs,
        [FromServices] IDateTimeProvider clock,
        CancellationToken cancellationToken)
    {
        var syncRun = OrderSyncRun.Queue(clock.UtcNow);
        await syncRuns.AddAsync(syncRun, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var backgroundJobId = backgroundJobs.Enqueue<OrderSyncBackgroundJob>(job =>
                job.ExecuteAsync(
                    syncRun.Id,
                    request.PageStart,
                    request.PageLimit,
                    request.MaxPages));

            syncRun.AttachBackgroundJob(backgroundJobId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Accepted(new OrderSyncQueuedResponse(syncRun.Id, backgroundJobId, syncRun.Status));
        }
        catch (Exception ex)
        {
            syncRun.MarkFailed(ex.Message, clock.UtcNow);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    [HttpGet]
    public async Task<ActionResult<OrderListResponse>> GetOrders(
        [FromQuery] GetOrdersRequest request,
        [FromServices] GetOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new GetOrdersQuery(
                request.Keyword,
                request.Status,
                request.PaymentStatus,
                request.ReturnRequestStatus,
                request.RefundRequestStatus,
                request.DateFrom,
                request.DateTo,
                request.ShippingChannel,
                request.SalesChannel,
                request.SortBy,
                request.SortDirection,
                request.Page,
                request.PageSize),
            cancellationToken));
    }

    [HttpPost("bulk/export")]
    public async Task<ActionResult<OrderBulkExportResponse>> BulkExport(
        [FromBody] OrderBulkExportRequest request,
        [FromServices] OrderingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (request.OrderIds.Count == 0)
            throw new BadRequestException("At least one order id is required.");
        if (request.OrderIds.Count > 500)
            throw new BadRequestException("Cannot export more than 500 orders at a time.");

        var orderIds = request.OrderIds.Distinct().ToArray();
        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.Id))
            .OrderByDescending(x => x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc)
            .Select(x => new OrderBulkExportItemResponse(
                x.Id,
                x.Number,
                x.ZortOrderId,
                x.CustomerName,
                x.CustomerPhone,
                x.CustomerEmail,
                x.Status,
                x.PaymentStatus,
                x.Amount,
                x.DiscountAmount,
                x.ShippingAmount,
                x.PaymentAmount,
                x.ShippingChannel,
                x.TrackingNo,
                x.SalesChannel,
                x.OrderDate,
                x.CreatedAtUtc,
                x.LastSyncedAt))
            .ToArrayAsync(cancellationToken);

        return Ok(new OrderBulkExportResponse(orders));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        [FromServices] CancelOrderHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new CancelOrderCommand(id, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve-manual-refund")]
    public async Task<IActionResult> ApproveManualRefund(
        Guid id,
        [FromBody] ApproveManualRefundRequest request,
        [FromServices] ApproveManualRefundHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ApproveManualRefundCommand(
                id,
                request.Reason,
                request.RefundReference,
                request.RefundedAmount),
            cancellationToken);
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

    [HttpGet("{id:guid}/return-request")]
    public async Task<ActionResult<OrderReturnRequestResponse>> GetReturnRequest(
        Guid id,
        [FromServices] GetOrderReturnRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var returnRequest = await handler.HandleAsync(
            new GetOrderReturnRequestQuery(id),
            cancellationToken);

        return returnRequest is null
            ? NoContent()
            : Ok(returnRequest);
    }

    [HttpPatch("{id:guid}/return-request")]
    public async Task<ActionResult<OrderReturnRequestResponse>> UpdateReturnRequest(
        Guid id,
        [FromBody] UpdateOrderReturnRequestRequest request,
        [FromServices] UpdateOrderReturnRequestHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new UpdateOrderReturnRequestCommand(id, request.Status, request.AdminNote),
            cancellationToken));
    }
}

public sealed record OrderSyncQueuedResponse(Guid SyncRunId, string BackgroundJobId, string Status);

public sealed record OrderBulkExportRequest(IReadOnlyCollection<Guid> OrderIds);

public sealed record OrderBulkExportResponse(IReadOnlyCollection<OrderBulkExportItemResponse> Items);

public sealed record OrderBulkExportItemResponse(
    Guid Id,
    string Number,
    long ZortOrderId,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    string Status,
    string PaymentStatus,
    decimal Amount,
    decimal DiscountAmount,
    decimal ShippingAmount,
    decimal PaymentAmount,
    string? ShippingChannel,
    string? TrackingNo,
    string SalesChannel,
    DateTime? OrderDate,
    DateTime CreatedAtUtc,
    DateTime LastSyncedAt);
