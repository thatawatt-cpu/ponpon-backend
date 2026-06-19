using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.GetPublishedHomeSlides;

public sealed class GetPublishedHomeSlidesHandler
{
    private readonly IHomeSlideRepository _slides;
    private readonly IDateTimeProvider _clock;

    public GetPublishedHomeSlidesHandler(IHomeSlideRepository slides, IDateTimeProvider clock)
    {
        _slides = slides;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<HomeSlideResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var slides = await _slides.GetPublishedAsync(_clock.UtcNow, cancellationToken);
        return slides.Select(HomeSlideResponse.From).ToArray();
    }
}
