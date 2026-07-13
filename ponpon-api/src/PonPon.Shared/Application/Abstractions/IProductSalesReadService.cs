namespace PonPon.Shared.Application.Abstractions;

public interface IProductSalesReadService
{
    Task<IReadOnlyDictionary<Guid, int>> GetSoldCountsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
}
