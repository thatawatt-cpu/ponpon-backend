using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Reviews.Application;

namespace PonPon.Modules.Reviews.Controllers;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin")]
public sealed class AdminReviewsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminReviewListResponse>> GetReviews(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? userId,
        [FromQuery] string? status,
        [FromQuery] int? rating,
        [FromQuery] bool includeDeleted,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] ReviewService service,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAdminReviewsAsync(
            productId,
            userId,
            status,
            rating,
            includeDeleted,
            page,
            pageSize,
            cancellationToken));
    }

    [HttpGet("{reviewId:guid}")]
    public async Task<ActionResult<AdminReviewDetailResponse>> GetReviewById(
        Guid reviewId,
        [FromServices] ReviewService service,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAdminReviewByIdAsync(reviewId, cancellationToken));
    }

    [HttpPatch("{reviewId:guid}/status")]
    public async Task<ActionResult<ReviewResponse>> UpdateStatus(
        Guid reviewId,
        [FromBody] UpdateReviewStatusRequest request,
        [FromServices] ReviewService service,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateReviewStatusAsync(reviewId, request.Status, cancellationToken));
    }

    [HttpPatch("bulk/status")]
    public async Task<ActionResult<BulkUpdateReviewStatusResponse>> BulkUpdateStatus(
        [FromBody] BulkUpdateReviewStatusRequest request,
        [FromServices] ReviewService service,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateReviewStatusesAsync(request.ReviewIds, request.Status, cancellationToken));
    }

    [HttpDelete("{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(
        Guid reviewId,
        [FromServices] ReviewService service,
        CancellationToken cancellationToken)
    {
        await service.AdminDeleteReviewAsync(reviewId, cancellationToken);
        return NoContent();
    }
}
