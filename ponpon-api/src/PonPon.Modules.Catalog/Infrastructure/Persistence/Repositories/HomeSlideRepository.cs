using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.HomeSlides;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class HomeSlideRepository : IHomeSlideRepository
{
    private readonly CatalogDbContext _dbContext;

    public HomeSlideRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyCollection<HomeSlide>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.HomeSlides
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<HomeSlide>> GetPublishedAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        return await _dbContext.HomeSlides
            .AsNoTracking()
            .Where(x => x.Status == HomeSlideStatus.Active)
            .Where(x => x.StartsAt == null || x.StartsAt <= now)
            .Where(x => x.EndsAt == null || x.EndsAt > now)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public Task<HomeSlide?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.HomeSlides.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<int> CountActiveQuotaAsync(DateTime now, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.HomeSlides
            .Where(x => !exceptId.HasValue || x.Id != exceptId.Value)
            .Where(x => x.Status != HomeSlideStatus.Inactive)
            .Where(x => x.EndsAt == null || x.EndsAt > now)
            .CountAsync(cancellationToken);
    }

    public Task<bool> SortOrderExistsAsync(int sortOrder, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.HomeSlides.AnyAsync(x => x.SortOrder == sortOrder && (!exceptId.HasValue || x.Id != exceptId.Value), cancellationToken);
    }

    public async Task AddAsync(HomeSlide slide, CancellationToken cancellationToken = default)
    {
        await _dbContext.HomeSlides.AddAsync(slide, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _dbContext.HomeSlides.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}
