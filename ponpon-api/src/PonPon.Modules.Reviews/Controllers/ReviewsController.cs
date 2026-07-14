using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;
using PonPon.Modules.Reviews.Application;
using PonPon.Modules.Reviews.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Reviews.Controllers;

[ApiController]
public sealed class ReviewsController : ControllerBase
{
    [HttpGet("api/products/{productId:guid}/reviews")]
    public async Task<ActionResult<IReadOnlyCollection<PublicReviewResponse>>> GetProductReviews(
        Guid productId,
        [FromServices] ReviewService service,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetProductReviewsAsync(productId, cancellationToken));
    }

    [HttpGet("api/products/{productId:guid}/reviews/summary")]
    public async Task<ActionResult<ProductReviewSummaryResponse>> GetProductReviewSummary(
        Guid productId,
        [FromServices] ReviewService service,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetProductSummaryAsync(productId, cancellationToken));
    }

    [Authorize]
    [Consumes("multipart/form-data")]
    [HttpPost("api/order-items/{orderItemId:guid}/review")]
    public async Task<ActionResult<ReviewResponse>> CreateReview(
        Guid orderItemId,
        [FromForm] CreateReviewFormRequest request,
        [FromServices] ReviewService service,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(currentUser, out var customerId))
        {
            return Forbid();
        }

        var streams = new List<Stream>();
        try
        {
            var uploads = (request.Media ?? [])
                .Select(media => ToUpload(media, streams))
                .ToArray();
            return Ok(await service.CreateReviewWithUploadsAsync(
                orderItemId,
                customerId,
                request.Rating,
                request.Comment,
                request.IsAnonymous,
                uploads,
                cancellationToken));
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    [Authorize]
    [Consumes("multipart/form-data")]
    [HttpPatch("api/reviews/{reviewId:guid}")]
    public async Task<ActionResult<ReviewResponse>> UpdateReview(
        Guid reviewId,
        [FromForm] UpdateReviewFormRequest request,
        [FromServices] ReviewService service,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(currentUser, out var customerId))
        {
            return Forbid();
        }

        var streams = new List<Stream>();
        try
        {
            var uploads = (request.Media ?? [])
                .Select(media => ToUpload(media, streams))
                .ToArray();
            return Ok(await service.UpdateReviewWithUploadsAsync(
                reviewId,
                customerId,
                request.Rating,
                request.Comment,
                request.IsAnonymous,
                uploads,
                cancellationToken));
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    [Authorize]
    [HttpDelete("api/reviews/{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(
        Guid reviewId,
        [FromServices] ReviewService service,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(currentUser, out var customerId))
        {
            return Forbid();
        }

        await service.DeleteReviewAsync(reviewId, customerId, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("api/reviews/media/upload-url")]
    public async Task<ActionResult<ReviewMediaUploadUrlResponse>> CreateMediaUploadUrl(
        [FromBody] ReviewMediaUploadUrlRequest request,
        [FromServices] ReviewService service,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(currentUser, out var customerId))
        {
            return Forbid();
        }

        return Ok(await service.CreateMediaUploadUrlAsync(customerId, request, cancellationToken));
    }

    [Authorize]
    [HttpPost("api/reviews/media/{mediaId:guid}/complete")]
    public async Task<ActionResult<ReviewMediaResponse>> CompleteMedia(
        Guid mediaId,
        [FromBody] CompleteReviewMediaRequest request,
        [FromServices] ReviewService service,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(currentUser, out var customerId))
        {
            return Forbid();
        }

        return Ok(await service.CompleteMediaAsync(mediaId, customerId, request, cancellationToken));
    }

    [HttpGet("api/reviews/media/{mediaId:guid}/status")]
    public async Task<ActionResult<ReviewMediaResponse>> GetMediaStatus(
        Guid mediaId,
        [FromServices] ReviewService service,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetMediaStatusAsync(mediaId, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("api/reviews/media/{mediaId:guid}/file")]
    public async Task<IActionResult> GetMediaFile(
        Guid mediaId,
        [FromServices] ReviewsDbContext reviews,
        [FromServices] ISupabaseStorageService storage,
        [FromServices] IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var media = await reviews.ReviewMedia
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == mediaId && x.DeletedAtUtc == null, cancellationToken)
            ?? throw new NotFoundException("Review media was not found.");

        if (string.IsNullOrWhiteSpace(media.MimeType))
        {
            throw new NotFoundException("Review media content type was not found.");
        }

        var url = media.Url.Trim();
        if (!IsAbsoluteHttpUrl(url))
        {
            url = await storage.GetPublicUrlAsync(url, cancellationToken);
        }

        using var response = await httpClientFactory.CreateClient().GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new NotFoundException("Review media file was not found.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        Response.Headers.CacheControl = "public, max-age=3600";
        return File(bytes, media.MimeType);
    }

    private static bool TryGetCustomerId(ICurrentUser currentUser, out Guid customerId)
    {
        customerId = currentUser.CustomerId.GetValueOrDefault();
        return currentUser.IsAuthenticated
            && currentUser.UserType == "Customer"
            && currentUser.CustomerId.HasValue;
    }

    private static CreateReviewUploadMediaRequest ToUpload(
        CreateReviewFormMediaRequest media,
        ICollection<Stream> streams)
    {
        if (media.File is null || media.File.Length == 0)
        {
            throw new BadRequestException("Review media file is required.");
        }

        var stream = media.File.OpenReadStream();
        streams.Add(stream);
        return new CreateReviewUploadMediaRequest(
            media.Type ?? string.Empty,
            stream,
            media.File.FileName,
            media.File.ContentType,
            media.File.Length,
            media.DurationSec,
            media.SortOrder);
    }

    private static bool IsAbsoluteHttpUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
