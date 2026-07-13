using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;

public sealed class ProductSalesReadService : IProductSalesReadService
{
    private readonly OrderingDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductSalesReadService> _logger;

    public ProductSalesReadService(
        OrderingDbContext dbContext,
        IMemoryCache cache,
        ILogger<ProductSalesReadService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetSoldCountsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, int>();

        var cached = new Dictionary<Guid, int>();
        var misses = new List<Guid>(ids.Length);
        foreach (var id in ids)
        {
            if (_cache.TryGetValue(CacheKey(id), out int soldCount))
                cached[id] = soldCount;
            else
                misses.Add(id);
        }

        if (misses.Count == 0)
            return cached;

        try
        {
            var rows = await (
                from item in _dbContext.OrderItems.AsNoTracking()
                join order in _dbContext.Orders.AsNoTracking() on item.OrderId equals order.Id
                where item.ProductId.HasValue
                   && misses.Contains(item.ProductId.Value)
                   && order.Status != "Voided"
                   && order.Status != "2"
                   && order.Status != "Returned"
                   && order.Status != "4"
                   && (order.PaymentStatus == "Paid" || order.PaymentStatus == "1")
                group item by item.ProductId!.Value into soldItems
                select new
                {
                    ProductId = soldItems.Key,
                    SoldCount = soldItems.Sum(x => x.Quantity)
                })
                .ToArrayAsync(cancellationToken);

            var queried = rows.ToDictionary(
                x => x.ProductId,
                x => x.SoldCount <= 0
                    ? 0
                    : decimal.ToInt32(decimal.Truncate(x.SoldCount)));
            foreach (var id in misses)
            {
                var soldCount = queried.GetValueOrDefault(id);
                cached[id] = soldCount;
                _cache.Set(CacheKey(id), soldCount, CacheOptions);
            }

            return cached;
        }
        catch (NpgsqlException exception)
        {
            _logger.LogWarning(
                exception,
                "Unable to load product sold counts for {ProductCount} products.",
                ids.Length);
            return new Dictionary<Guid, int>();
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                "Unable to load product sold counts for {ProductCount} products.",
                ids.Length);
            return new Dictionary<Guid, int>();
        }
    }

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
        SlidingExpiration = TimeSpan.FromSeconds(10),
        Size = 1
    };

    private static string CacheKey(Guid productId) => $"product-sold-count:{productId:N}";
}
