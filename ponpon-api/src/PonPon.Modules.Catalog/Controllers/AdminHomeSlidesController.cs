using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Features.HomeSlides;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.CreateHomeSlide;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.DeleteHomeSlide;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.GetHomeSlides;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.ReorderHomeSlides;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.UpdateHomeSlide;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[Route("api/admin/home-slides")]
[Authorize(Roles = "Admin")]
public sealed class AdminHomeSlidesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<HomeSlideResponse>>> GetAll([FromServices] GetHomeSlidesHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<HomeSlideResponse>> Create([FromBody] HomeSlideRequest request, [FromServices] CreateHomeSlideHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateHomeSlideCommand(
                request.Image,
                request.Badge,
                request.Title,
                request.Description,
                request.LinkUrl,
                request.CtaLabel,
                request.Status,
                request.StartsAt,
                request.EndsAt,
                request.SortOrder),
            cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<HomeSlideResponse>> Update(Guid id, [FromBody] HomeSlideRequest request, [FromServices] UpdateHomeSlideHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new UpdateHomeSlideCommand(
                id,
                request.Image,
                request.Badge,
                request.Title,
                request.Description,
                request.LinkUrl,
                request.CtaLabel,
                request.Status,
                request.StartsAt,
                request.EndsAt,
                request.SortOrder),
            cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] DeleteHomeSlideHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteHomeSlideCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("reorder")]
    public async Task<ActionResult<IReadOnlyCollection<HomeSlideResponse>>> Reorder([FromBody] ReorderHomeSlidesRequest request, [FromServices] ReorderHomeSlidesHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new ReorderHomeSlidesCommand(request.Slides.Select(x => (x.Id, x.SortOrder)).ToArray()),
            cancellationToken));
    }
}
