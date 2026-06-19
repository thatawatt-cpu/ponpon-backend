namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed record ZortGetOrdersResponse(
    IReadOnlyCollection<ZortOrderDto> Orders,
    int? Count,
    decimal? TotalAmount,
    decimal? TotalPaymentAmount,
    string? ResCode,
    string? ResDesc);
