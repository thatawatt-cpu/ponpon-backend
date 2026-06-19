using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.DeleteHomeSlide;

public sealed class DeleteHomeSlideHandler
{
    private readonly IHomeSlideRepository _slides;

    public DeleteHomeSlideHandler(IHomeSlideRepository slides) => _slides = slides;

    public async Task HandleAsync(DeleteHomeSlideCommand command, CancellationToken cancellationToken = default)
    {
        _ = await _slides.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Home slide was not found.");

        await _slides.DeleteAsync(command.Id, cancellationToken);
    }
}
