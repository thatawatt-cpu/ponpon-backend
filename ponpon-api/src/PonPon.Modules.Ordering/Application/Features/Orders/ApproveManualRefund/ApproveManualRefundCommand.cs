namespace PonPon.Modules.Ordering.Application.Features.Orders.ApproveManualRefund;

public sealed record ApproveManualRefundRequest(
    string Reason,
    string? RefundReference,
    decimal? RefundedAmount);

public sealed record ApproveManualRefundCommand(
    Guid OrderId,
    string Reason,
    string? RefundReference,
    decimal? RefundedAmount);
