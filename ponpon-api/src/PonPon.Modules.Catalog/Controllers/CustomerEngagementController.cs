using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Features.CustomerEngagement;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[Authorize]
[Route("api/customers/me")]
public sealed class CustomerEngagementController : ControllerBase
{
    [HttpGet("wishlist")]
    public async Task<ActionResult<WishlistResponse>> GetWishlist(
        [FromServices] CustomerEngagementService service,
        CancellationToken cancellationToken)
        => Ok(await service.GetWishlistAsync(cancellationToken));

    [HttpPost("wishlist/{productId:guid}")]
    public async Task<ActionResult<WishlistResponse>> AddWishlist(
        Guid productId,
        [FromServices] CustomerEngagementService service,
        CancellationToken cancellationToken)
        => Ok(await service.AddWishlistAsync(productId, cancellationToken));

    [HttpDelete("wishlist/{productId:guid}")]
    public async Task<ActionResult<WishlistResponse>> DeleteWishlist(
        Guid productId,
        [FromServices] CustomerEngagementService service,
        CancellationToken cancellationToken)
        => Ok(await service.DeleteWishlistAsync(productId, cancellationToken));

    [HttpGet("recently-viewed")]
    public async Task<ActionResult<RecentlyViewedResponse>> GetRecentlyViewed(
        [FromServices] CustomerEngagementService service,
        CancellationToken cancellationToken)
        => Ok(await service.GetRecentlyViewedAsync(cancellationToken));

    [HttpPost("recently-viewed")]
    public async Task<ActionResult<RecentlyViewedResponse>> AddRecentlyViewed(
        [FromBody] AddRecentlyViewedRequest request,
        [FromServices] CustomerEngagementService service,
        CancellationToken cancellationToken)
        => Ok(await service.AddRecentlyViewedAsync(request, cancellationToken));
}
