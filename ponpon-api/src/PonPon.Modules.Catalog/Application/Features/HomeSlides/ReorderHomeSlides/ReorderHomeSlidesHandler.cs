using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.ReorderHomeSlides;

public sealed class ReorderHomeSlidesHandler
{
    private readonly IHomeSlideRepository _slides;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ReorderHomeSlidesHandler(IHomeSlideRepository slides, ICatalogUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _slides = slides;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<HomeSlideResponse>> HandleAsync(ReorderHomeSlidesCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Slides.Count == 0)
            throw new BadRequestException("Slides are required.");

        if (command.Slides.Select(x => x.Id).Distinct().Count() != command.Slides.Count)
            throw new BadRequestException("Slide ids must not be duplicated.");

        if (command.Slides.Any(x => x.SortOrder < 1))
            throw new BadRequestException("Sort order must be greater than zero.");

        if (command.Slides.Select(x => x.SortOrder).Distinct().Count() != command.Slides.Count)
            throw new BadRequestException("Sort order must not be duplicated.");

        var existingSlides = await _slides.GetAllAsync(cancellationToken);

        if (existingSlides.Count != command.Slides.Count || existingSlides.Select(x => x.Id).Except(command.Slides.Select(x => x.Id)).Any())
            throw new BadRequestException("Reorder must include every existing home slide exactly once.");

        var sortOrdersById = command.Slides.ToDictionary(x => x.Id, x => x.SortOrder);
        var now = _clock.UtcNow;

        foreach (var slide in existingSlides)
            slide.SetSortOrder(sortOrdersById[slide.Id], now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existingSlides
            .OrderBy(x => x.SortOrder)
            .Select(HomeSlideResponse.From)
            .ToArray();
    }
}
