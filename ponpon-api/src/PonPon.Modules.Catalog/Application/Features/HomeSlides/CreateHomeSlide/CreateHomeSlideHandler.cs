using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.HomeSlides;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.CreateHomeSlide;

public sealed class CreateHomeSlideHandler
{
    private readonly IHomeSlideRepository _slides;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CreateHomeSlideHandler(IHomeSlideRepository slides, ICatalogUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _slides = slides;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<HomeSlideResponse> HandleAsync(CreateHomeSlideCommand command, CancellationToken cancellationToken = default)
    {
        HomeSlideValidator.ValidateFields(command.Image, command.Title, command.LinkUrl, command.Status, command.StartsAt, command.EndsAt, command.SortOrder);

        var now = _clock.UtcNow;
        await HomeSlideValidator.EnsureActiveQuotaAvailableAsync(_slides, command.Status, command.EndsAt, now, null, cancellationToken);
        await HomeSlideValidator.EnsureSortOrderAvailableAsync(_slides, command.SortOrder, null, cancellationToken);

        var slide = HomeSlide.Create(
            command.Image.Trim(),
            command.Badge?.Trim() ?? string.Empty,
            command.Title.Trim(),
            command.Description?.Trim() ?? string.Empty,
            command.LinkUrl.Trim(),
            command.CtaLabel?.Trim() ?? string.Empty,
            command.Status,
            command.StartsAt,
            command.EndsAt,
            command.SortOrder,
            now);

        await _slides.AddAsync(slide, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return HomeSlideResponse.From(slide);
    }
}
