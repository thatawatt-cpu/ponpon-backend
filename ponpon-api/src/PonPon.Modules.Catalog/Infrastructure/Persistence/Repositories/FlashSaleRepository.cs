using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.FlashSales;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class FlashSaleRepository : IFlashSaleRepository
{
    private readonly CatalogDbContext _dbContext;

    public FlashSaleRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyCollection<FlashSale>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.FlashSales
            .AsNoTracking()
            .Include(x => x.Products)
            .OrderByDescending(x => x.StartDate)
            .ToArrayAsync(cancellationToken);
    }

    public Task<FlashSale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.FlashSales
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<FlashSale?> GetActiveAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        return _dbContext.FlashSales
            .AsNoTracking()
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.StartDate <= today && today <= x.EndDate, cancellationToken);
    }

    public async Task<IReadOnlyCollection<FlashSale>> GetActiveForProductsAsync(
        DateOnly today,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        return await _dbContext.FlashSales
            .AsNoTracking()
            .Include(x => x.Products)
            .Where(x => x.IsActive
                        && x.StartDate <= today
                        && today <= x.EndDate
                        && x.Products.Any(p => ids.Contains(p.ProductId)))
            .OrderByDescending(x => x.StartDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(FlashSale flashSale, CancellationToken cancellationToken = default)
    {
        await _dbContext.FlashSales.AddAsync(flashSale, cancellationToken);
    }

    public async Task DeleteProductsAsync(Guid flashSaleId, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.FlashSaleReservations.AnyAsync(
                x => x.FlashSaleId == flashSaleId && !x.IsReleased, cancellationToken))
            throw new BadRequestException("Flash sale with active order reservations cannot be updated.");

        await _dbContext.FlashSaleProducts.Where(x => x.FlashSaleId == flashSaleId).ExecuteDeleteAsync(cancellationToken);

        var tracked = _dbContext.ChangeTracker.Entries<FlashSaleProduct>()
            .Where(e => e.Entity.FlashSaleId == flashSaleId)
            .ToList();

        foreach (var entry in tracked)
            entry.State = EntityState.Detached;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.FlashSaleReservations.AnyAsync(
                x => x.FlashSaleId == id && !x.IsReleased, cancellationToken))
            throw new BadRequestException("Flash sale with active order reservations cannot be deleted.");
        await _dbContext.FlashSales.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> TryReserveQuotaAsync(
        Guid orderId,
        Guid flashSaleId,
        IReadOnlyDictionary<Guid, int> productQuantities,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (productQuantities.Count == 0) return true;
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = BitConverter.ToInt64(flashSaleId.ToByteArray(), 0);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);

        if (await _dbContext.FlashSaleReservations.AnyAsync(x => x.OrderId == orderId && !x.IsReleased, cancellationToken))
        {
            await tx.CommitAsync(cancellationToken);
            return true;
        }

        var productIds = productQuantities.Keys.ToArray();
        var entries = await _dbContext.FlashSaleProducts
            .Where(x => x.FlashSaleId == flashSaleId && productIds.Contains(x.ProductId))
            .ToArrayAsync(cancellationToken);
        if (entries.Length != productIds.Length
            || entries.Any(x => x.QuantityLimit.HasValue
                && x.ReservedQuantity + productQuantities[x.ProductId] > x.QuantityLimit.Value))
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        foreach (var entry in entries)
        {
            var quantity = productQuantities[entry.ProductId];
            _dbContext.Entry(entry).Property(x => x.ReservedQuantity).CurrentValue += quantity;
            await _dbContext.FlashSaleReservations.AddAsync(
                new FlashSaleReservation(orderId, flashSaleId, entry.ProductId, quantity, nowUtc),
                cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task ReleaseQuotaByOrderAsync(
        Guid orderId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var reservations = await _dbContext.FlashSaleReservations
            .Where(x => x.OrderId == orderId && !x.IsReleased)
            .ToArrayAsync(cancellationToken);
        foreach (var flashSaleId in reservations.Select(x => x.FlashSaleId).Distinct().OrderBy(x => x))
        {
            var lockKey = BitConverter.ToInt64(flashSaleId.ToByteArray(), 0);
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);
        }
        foreach (var reservation in reservations)
        {
            var entry = await _dbContext.FlashSaleProducts.FirstOrDefaultAsync(
                x => x.FlashSaleId == reservation.FlashSaleId && x.ProductId == reservation.ProductId,
                cancellationToken);
            if (entry is not null)
                _dbContext.Entry(entry).Property(x => x.ReservedQuantity).CurrentValue =
                    Math.Max(0, entry.ReservedQuantity - reservation.Quantity);
            reservation.Release(nowUtc);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
