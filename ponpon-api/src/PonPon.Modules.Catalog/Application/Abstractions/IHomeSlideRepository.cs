using PonPon.Modules.Catalog.Domain.HomeSlides;

namespace PonPon.Modules.Catalog.Application.Abstractions;

public interface IHomeSlideRepository
{
    Task<IReadOnlyCollection<HomeSlide>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<HomeSlide>> GetPublishedAsync(DateTime now, CancellationToken cancellationToken = default);
    Task<HomeSlide?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountActiveQuotaAsync(DateTime now, Guid? exceptId = null, CancellationToken cancellationToken = default);
    Task<bool> SortOrderExistsAsync(int sortOrder, Guid? exceptId = null, CancellationToken cancellationToken = default);
    Task AddAsync(HomeSlide slide, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
