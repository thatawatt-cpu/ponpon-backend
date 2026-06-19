using PonPon.Modules.Catalog.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.GetHomeSlides;

public sealed class GetHomeSlidesHandler
{
    private readonly IHomeSlideRepository _slides;

    public GetHomeSlidesHandler(IHomeSlideRepository slides) => _slides = slides;

    public async Task<IReadOnlyCollection<HomeSlideResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var slides = await _slides.GetAllAsync(cancellationToken);
        return slides.Select(HomeSlideResponse.From).ToArray();
    }
}
