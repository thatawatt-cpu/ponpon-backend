using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Features.HomeSlides;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.GetPublishedHomeSlides;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[Route("api/home-slides")]
public sealed class HomeSlidesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<HomeSlideResponse>>> GetPublished([FromServices] GetPublishedHomeSlidesHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }
}
