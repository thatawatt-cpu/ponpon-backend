namespace PonPon.Modules.Ordering.Application.Features.Orders.ReturnOrder;

public sealed record ReturnEvidenceFile(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);

public sealed record CreateOrderReturnRequestCommand(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    IReadOnlyCollection<ReturnEvidenceFile> EvidenceFiles);

public sealed record GetMyOrderReturnRequestQuery(Guid OrderId, Guid CustomerId);

public sealed record GetOrderReturnRequestQuery(Guid OrderId);

public sealed record UpdateOrderReturnRequestRequest(string Status, string? AdminNote);

public sealed record UpdateOrderReturnRequestCommand(
    Guid OrderId,
    string Status,
    string? AdminNote);

public sealed record OrderReturnRequestResponse(
    Guid Id,
    Guid OrderId,
    string Reason,
    string Status,
    IReadOnlyCollection<string> EvidenceImageUrls,
    string? AdminNote,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
