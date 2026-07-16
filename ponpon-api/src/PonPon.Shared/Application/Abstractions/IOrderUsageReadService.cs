namespace PonPon.Shared.Application.Abstractions;

public interface IOrderUsageReadService
{
    Task<IReadOnlyDictionary<Guid, OrderUsageDetails>> GetByIdsAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken = default);
}

public sealed record OrderUsageDetails(
    Guid OrderId,
    string OrderNumber,
    string? CustomerName,
    string? CustomerPhone,
    decimal OrderTotal);
