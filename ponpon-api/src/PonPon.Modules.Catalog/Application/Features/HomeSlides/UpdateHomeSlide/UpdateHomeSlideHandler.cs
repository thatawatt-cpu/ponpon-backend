using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.UpdateHomeSlide;

public sealed class UpdateHomeSlideHandler
{
    private readonly IHomeSlideRepository _slides;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateHomeSlideHandler(IHomeSlideRepository slides, ICatalogUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _slides = slides;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<HomeSlideResponse> HandleAsync(UpdateHomeSlideCommand command, CancellationToken cancellationToken = default)
    {
        HomeSlideValidator.ValidateFields(command.Image, command.Title, command.LinkUrl, command.Status, command.StartsAt, command.EndsAt, command.SortOrder);

        var slide = await _slides.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Home slide was not found.");

        var now = _clock.UtcNow;
        await HomeSlideValidator.EnsureActiveQuotaAvailableAsync(_slides, command.Status, command.EndsAt, now, command.Id, cancellationToken);
        await HomeSlideValidator.EnsureSortOrderAvailableAsync(_slides, command.SortOrder, command.Id, cancellationToken);

        slide.Update(
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return HomeSlideResponse.From(slide);
    }
}
