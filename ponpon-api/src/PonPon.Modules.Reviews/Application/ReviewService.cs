using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;
using PonPon.Modules.Identity.Infrastructure.Persistence;
using PonPon.Modules.Ordering.Application;
using PonPon.Modules.Ordering.Infrastructure.Persistence;
using PonPon.Modules.Reviews.Domain;
using PonPon.Modules.Reviews.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Reviews.Application;

public sealed class ReviewService
{
    private const int MinRating = 1;
    private const int MaxRating = 5;
    private const int MinCommentLength = 10;
    private const int MaxCommentLength = 1000;
    private const int MaxMedia = 5;
    private const int MaxImages = 5;
    private const int MaxVideos = 3;
    private const int MinVideoDurationSec = 1;
    private const int MaxVideoDurationSec = 60;
    private static readonly HashSet<string> SupportedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
    private static readonly HashSet<string> SupportedVideoMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/quicktime",
        "video/webm"
    };
    private static readonly TimeSpan EditWindow = TimeSpan.FromDays(30);

    private readonly ReviewsDbContext _reviews;
    private readonly OrderingDbContext _ordering;
    private readonly IdentityDbContext _identity;
    private readonly IDateTimeProvider _clock;
    private readonly ISupabaseStorageService _storage;

    public ReviewService(
        ReviewsDbContext reviews,
        OrderingDbContext ordering,
        IdentityDbContext identity,
        IDateTimeProvider clock,
        ISupabaseStorageService storage)
    {
        _reviews = reviews;
        _ordering = ordering;
        _identity = identity;
        _clock = clock;
        _storage = storage;
    }

    public async Task<IReadOnlyCollection<PublicReviewResponse>> GetProductReviewsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _reviews.Reviews
            .AsNoTracking()
            .Include(x => x.Media)
            .Where(x => x.ProductId == productId
                        && x.Status == ReviewStatus.Published
                        && x.DeletedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        var customerIds = reviews
            .Where(x => !x.IsAnonymous)
            .Select(x => x.UserId)
            .Distinct()
            .ToArray();
        var profiles = await _identity.Customers
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .Select(x => new CustomerPublicProfile(x.Id, x.LineProfile.DisplayName, x.LineProfile.PictureUrl))
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        return reviews
            .Select(x => ToPublicResponse(
                x,
                x.IsAnonymous ? null : profiles.GetValueOrDefault(x.UserId)))
            .ToArray();
    }

    public async Task<AdminReviewListResponse> GetAdminReviewsAsync(
        Guid? productId,
        Guid? userId,
        string? status,
        int? rating,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _reviews.Reviews
            .AsNoTracking()
            .Include(x => x.Media)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(x => x.DeletedAtUtc == null);
        }

        if (productId is Guid product)
        {
            query = query.Where(x => x.ProductId == product);
        }

        if (userId is Guid user)
        {
            query = query.Where(x => x.UserId == user);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            if (!ReviewStatus.IsValid(normalized))
            {
                throw new BadRequestException("Review status must be published or hidden.");
            }

            query = query.Where(x => x.Status == normalized);
        }

        if (rating is not null)
        {
            if (rating is < MinRating or > MaxRating)
            {
                throw new BadRequestException("Rating must be an integer from 1 to 5.");
            }

            query = query.Where(x => x.Rating == rating);
        }

        var total = await query.CountAsync(cancellationToken);
        var reviews = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new AdminReviewListResponse(
            reviews.Select(ToAdminResponse).ToArray(),
            total,
            page,
            pageSize);
    }

    public async Task<ReviewResponse> GetAdminReviewByIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var review = await _reviews.Reviews
            .AsNoTracking()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Review was not found.");

        return ToAdminResponse(review);
    }

    public async Task<ProductReviewSummaryResponse> GetProductSummaryAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var ratings = await _reviews.Reviews
            .AsNoTracking()
            .Where(x => x.ProductId == productId
                        && x.Status == ReviewStatus.Published
                        && x.DeletedAtUtc == null)
            .Select(x => x.Rating)
            .ToArrayAsync(cancellationToken);

        return new ProductReviewSummaryResponse(
            productId,
            ratings.Length,
            ratings.Length == 0 ? 0 : Math.Round((decimal)ratings.Average(), 2),
            ratings.Count(x => x == 1),
            ratings.Count(x => x == 2),
            ratings.Count(x => x == 3),
            ratings.Count(x => x == 4),
            ratings.Count(x => x == 5));
    }

    public async Task<ReviewResponse> CreateReviewWithUploadsAsync(
        Guid orderItemId,
        Guid customerId,
        int rating,
        string? comment,
        bool isAnonymous,
        IReadOnlyCollection<CreateReviewUploadMediaRequest> uploads,
        CancellationToken cancellationToken = default)
    {
        var mediaToValidate = uploads
            .Select(x => new CreateReviewMediaRequest(
                x.Type,
                "pending-upload",
                null,
                x.DurationSec,
                x.FileSizeBytes,
                x.MimeType,
                x.SortOrder))
            .ToArray();
        ValidateReview(rating, comment ?? string.Empty, mediaToValidate);

        var context = await GetReviewableOrderItemAsync(orderItemId, customerId, cancellationToken);
        var media = new List<CreateReviewMediaRequest>(uploads.Count);
        var uploadedPaths = new List<string>(uploads.Count);
        try
        {
            foreach (var upload in uploads)
            {
                var path = BuildReviewMediaPath(orderItemId, upload.FileName);
                var url = await _storage.UploadAsync(path, upload.FileStream, upload.MimeType, cancellationToken);
                uploadedPaths.Add(path);
                media.Add(new CreateReviewMediaRequest(
                    upload.Type,
                    url,
                    null,
                    upload.DurationSec,
                    upload.FileSizeBytes,
                    upload.MimeType,
                    upload.SortOrder));
            }

            var now = _clock.UtcNow;
            var review = Review.Create(
                context.ProductId,
                context.VariantId,
                context.OrderId,
                orderItemId,
                customerId,
                rating,
                comment ?? string.Empty,
                isAnonymous,
                now);
            foreach (var item in media)
            {
                review.AddMedia(ToMedia(review.Id, item, now));
            }

            await _reviews.Reviews.AddAsync(review, cancellationToken);
            await _reviews.SaveChangesAsync(cancellationToken);
            return ToOwnerResponse(review);
        }
        catch
        {
            foreach (var path in uploadedPaths)
            {
                try
                {
                    await _storage.DeleteAsync(path, CancellationToken.None);
                }
                catch
                {
                    // Cleanup is best effort; keep the original upload or persistence failure.
                }
            }

            throw;
        }
    }

    public async Task<ReviewResponse> UpdateReviewWithUploadsAsync(
        Guid reviewId,
        Guid customerId,
        int rating,
        string? comment,
        bool isAnonymous,
        IReadOnlyCollection<CreateReviewUploadMediaRequest> uploads,
        CancellationToken cancellationToken = default)
    {
        var mediaToValidate = uploads
            .Select(x => new CreateReviewMediaRequest(
                x.Type,
                "pending-upload",
                null,
                x.DurationSec,
                x.FileSizeBytes,
                x.MimeType,
                x.SortOrder))
            .ToArray();
        ValidateReview(rating, comment ?? string.Empty, mediaToValidate);

        var review = await _reviews.Reviews
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Review was not found.");

        EnsureReviewEditable(review, customerId);

        var activeMedia = review.Media
            .Where(x => x.DeletedAtUtc == null)
            .Select(x => new CreateReviewMediaRequest(
                x.Type,
                x.Url,
                x.ThumbnailUrl,
                x.DurationSec,
                x.FileSizeBytes,
                x.MimeType,
                x.SortOrder))
            .Concat(mediaToValidate)
            .ToArray();
        ValidateMediaSet(activeMedia);

        var media = new List<CreateReviewMediaRequest>(uploads.Count);
        var uploadedPaths = new List<string>(uploads.Count);
        try
        {
            foreach (var upload in uploads)
            {
                var path = BuildReviewMediaPath(review.OrderItemId, upload.FileName);
                var url = await _storage.UploadAsync(path, upload.FileStream, upload.MimeType, cancellationToken);
                uploadedPaths.Add(path);
                media.Add(new CreateReviewMediaRequest(
                    upload.Type,
                    url,
                    null,
                    upload.DurationSec,
                    upload.FileSizeBytes,
                    upload.MimeType,
                    upload.SortOrder));
            }

            var now = _clock.UtcNow;
            review.Edit(rating, comment ?? string.Empty, isAnonymous, now);
            foreach (var item in media)
            {
                review.AddMedia(ToMedia(review.Id, item, now));
            }

            await _reviews.SaveChangesAsync(cancellationToken);
            return ToOwnerResponse(review);
        }
        catch
        {
            foreach (var path in uploadedPaths)
            {
                try
                {
                    await _storage.DeleteAsync(path, CancellationToken.None);
                }
                catch
                {
                    // Cleanup is best effort; keep the original upload or persistence failure.
                }
            }

            throw;
        }
    }

    public async Task DeleteReviewAsync(
        Guid reviewId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var review = await _reviews.Reviews
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Review was not found.");

        if (review.UserId != customerId)
        {
            throw new UnauthorizedException("Only the review owner can delete this review.");
        }

        if (review.DeletedAtUtc is null)
        {
            var now = _clock.UtcNow;
            review.SoftDelete(now);
            foreach (var media in review.Media.Where(x => x.DeletedAtUtc == null))
            {
                media.SoftDelete(now);
            }

            await _reviews.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ReviewResponse> UpdateReviewStatusAsync(
        Guid reviewId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!ReviewStatus.IsValid(normalized))
        {
            throw new BadRequestException("Review status must be published or hidden.");
        }

        var review = await _reviews.Reviews
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Review was not found.");

        if (review.DeletedAtUtc is not null)
        {
            throw new BadRequestException("Deleted reviews cannot be published or hidden.");
        }

        var now = _clock.UtcNow;
        if (normalized == ReviewStatus.Published)
        {
            review.Publish(now);
        }
        else
        {
            review.Hide(now);
        }

        await _reviews.SaveChangesAsync(cancellationToken);
        return ToAdminResponse(review);
    }

    public async Task AdminDeleteReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var review = await _reviews.Reviews
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Review was not found.");

        if (review.DeletedAtUtc is null)
        {
            var now = _clock.UtcNow;
            review.SoftDelete(now);
            foreach (var media in review.Media.Where(x => x.DeletedAtUtc == null))
            {
                media.SoftDelete(now);
            }

            await _reviews.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ReviewMediaUploadUrlResponse> CreateMediaUploadUrlAsync(
        Guid customerId,
        ReviewMediaUploadUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        var mediaRequest = new CreateReviewMediaRequest(
            request.Type,
            BuildPendingMediaUrl(request.ReviewId, request.FileName),
            null,
            request.DurationSec,
            request.FileSizeBytes,
            request.MimeType,
            request.SortOrder);

        ValidateMediaSet([mediaRequest]);

        var review = await _reviews.Reviews
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("Review was not found.");

        if (review.UserId != customerId)
        {
            throw new UnauthorizedException("Only the review owner can add media.");
        }

        if (review.DeletedAtUtc is not null || review.Status == ReviewStatus.Hidden)
        {
            throw new BadRequestException("Hidden or deleted reviews cannot be changed.");
        }

        var active = review.Media
            .Where(x => x.DeletedAtUtc == null)
            .Select(x => new CreateReviewMediaRequest(x.Type, x.Url, x.ThumbnailUrl, x.DurationSec, x.FileSizeBytes, x.MimeType, x.SortOrder))
            .Append(mediaRequest)
            .ToArray();
        ValidateMediaSet(active);

        var now = _clock.UtcNow;
        var media = ReviewMedia.CreatePendingUpload(
            review.Id,
            mediaRequest.Type,
            mediaRequest.Url,
            mediaRequest.ThumbnailUrl,
            mediaRequest.DurationSec,
            mediaRequest.FileSizeBytes,
            mediaRequest.MimeType,
            mediaRequest.SortOrder,
            now);
        review.AddMedia(media);
        await _reviews.SaveChangesAsync(cancellationToken);

        return new ReviewMediaUploadUrlResponse(
            media.Id,
            media.Url,
            media.Url,
            media.Status);
    }

    public async Task<ReviewMediaResponse> CompleteMediaAsync(
        Guid mediaId,
        Guid customerId,
        CompleteReviewMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        var review = await _reviews.Reviews
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Media.Any(media => media.Id == mediaId), cancellationToken)
            ?? throw new NotFoundException("Review media was not found.");

        if (review.UserId != customerId)
        {
            throw new UnauthorizedException("Only the review owner can complete this media.");
        }

        var media = review.Media.First(x => x.Id == mediaId);

        if (media.DeletedAtUtc is not null)
        {
            throw new BadRequestException("Deleted media cannot be completed.");
        }

        var now = _clock.UtcNow;
        if (request.Failed)
        {
            media.Fail(now);
        }
        else
        {
            if (media.Type == ReviewMediaType.Video
                && (request.DurationSec ?? media.DurationSec) is not (>= MinVideoDurationSec and <= MaxVideoDurationSec))
            {
                throw new BadRequestException("Video duration must be between 1 and 60 seconds.");
            }

            media.Complete(request.Url ?? media.Url, request.ThumbnailUrl, request.DurationSec, now);
        }

        await _reviews.SaveChangesAsync(cancellationToken);
        return ToMediaResponse(media);
    }

    public async Task<ReviewMediaResponse> GetMediaStatusAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var media = await _reviews.ReviewMedia
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == mediaId && x.DeletedAtUtc == null, cancellationToken)
            ?? throw new NotFoundException("Review media was not found.");

        return ToMediaResponse(media);
    }

    private static void ValidateReview(
        int rating,
        string comment,
        IReadOnlyCollection<CreateReviewMediaRequest>? media)
    {
        if (rating is < MinRating or > MaxRating)
        {
            throw new BadRequestException("Rating must be an integer from 1 to 5.");
        }

        var trimmed = comment?.Trim() ?? string.Empty;
        if (trimmed.Length is < MinCommentLength or > MaxCommentLength)
        {
            throw new BadRequestException("Comment length must be between 10 and 1000 characters.");
        }

        ValidateMediaSet(media ?? []);
    }

    private static void ValidateMediaSet(IReadOnlyCollection<CreateReviewMediaRequest> media)
    {
        if (media.Count > MaxMedia)
        {
            throw new BadRequestException("Each review supports at most 5 media items.");
        }

        var imageCount = 0;
        var videoCount = 0;
        foreach (var item in media)
        {
            var type = item.Type?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!ReviewMediaType.IsValid(type))
            {
                throw new BadRequestException("Review media type must be image or video.");
            }

            if (string.IsNullOrWhiteSpace(item.Url))
            {
                throw new BadRequestException("Review media URL is required.");
            }

            var mimeType = item.MimeType?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new BadRequestException("Review media MIME type is required.");
            }

            if (type == ReviewMediaType.Image)
            {
                if (!SupportedImageMimeTypes.Contains(mimeType))
                {
                    throw new BadRequestException("Image MIME type must be image/jpeg, image/png, or image/webp.");
                }

                if (item.DurationSec is not null)
                {
                    throw new BadRequestException("Image duration must be null or omitted.");
                }

                imageCount++;
            }
            else
            {
                if (!SupportedVideoMimeTypes.Contains(mimeType))
                {
                    throw new BadRequestException("Video MIME type must be video/mp4, video/quicktime, or video/webm.");
                }

                videoCount++;
                if (item.DurationSec is not (>= MinVideoDurationSec and <= MaxVideoDurationSec))
                {
                    throw new BadRequestException("Video duration must be between 1 and 60 seconds.");
                }
            }
        }

        if (imageCount > MaxImages)
        {
            throw new BadRequestException("Each review supports at most 5 images.");
        }

        if (videoCount > MaxVideos)
        {
            throw new BadRequestException("Each review supports at most 3 videos.");
        }
    }

    private void EnsureReviewEditable(Review review, Guid customerId)
    {
        if (review.UserId != customerId)
        {
            throw new UnauthorizedException("Only the review owner can edit this review.");
        }

        if (review.DeletedAtUtc is not null || review.Status == ReviewStatus.Hidden)
        {
            throw new BadRequestException("Hidden or deleted reviews cannot be edited.");
        }

        if (_clock.UtcNow - review.CreatedAtUtc > EditWindow)
        {
            throw new BadRequestException("Review can be edited only within 30 days after creation.");
        }
    }

    private static ReviewMedia ToMedia(Guid reviewId, CreateReviewMediaRequest request, DateTime now)
    {
        return ReviewMedia.Create(
            reviewId,
            request.Type,
            request.Url,
            request.ThumbnailUrl,
            request.DurationSec,
            request.FileSizeBytes,
            request.MimeType,
            request.SortOrder,
            now);
    }

    private async Task<ReviewableOrderItem> GetReviewableOrderItemAsync(
        Guid orderItemId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var order = await _ordering.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId && x.Items.Any(item => item.Id == orderItemId),
                cancellationToken)
            ?? throw new NotFoundException("Order item was not found.");

        if (!IsReviewableOrderStatus(order.Status))
        {
            throw new BadRequestException("Review can be created only after the order is delivered or completed.");
        }

        var orderItem = order.Items.First(x => x.Id == orderItemId);
        if (orderItem.ProductId is not Guid productId)
        {
            throw new BadRequestException("Order item is not linked to a product.");
        }

        var exists = await _reviews.Reviews.AnyAsync(
            x => x.OrderItemId == orderItemId && x.DeletedAtUtc == null,
            cancellationToken);
        if (exists)
        {
            throw new BadRequestException("A review already exists for this order item.");
        }

        return new ReviewableOrderItem(order.Id, productId, orderItem.VariantId);
    }

    private static bool IsReviewableOrderStatus(string status)
    {
        return string.Equals(status, "delivered", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ZortOrderStatus.Success.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ((int)ZortOrderStatus.Success).ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPendingMediaUrl(Guid reviewId, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        return $"reviews/{reviewId:D}/{Guid.NewGuid():D}{extension.ToLowerInvariant()}";
    }

    private static string BuildReviewMediaPath(Guid orderItemId, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        return $"reviews/{orderItemId:D}/{Guid.NewGuid():D}{extension.ToLowerInvariant()}";
    }

    private static PublicReviewResponse ToPublicResponse(Review review, CustomerPublicProfile? profile)
    {
        var media = review.Media
            .Where(x => x.DeletedAtUtc == null && x.Status == ReviewMediaStatus.Ready)
            .OrderBy(x => x.SortOrder)
            .Select(ToMediaResponse)
            .ToArray();
        return new PublicReviewResponse(
            review.Id,
            review.ProductId,
            review.VariantId,
            review.Rating,
            review.Comment,
            review.IsAnonymous,
            profile?.DisplayName,
            profile?.PictureUrl,
            media,
            review.EditedAtUtc,
            review.CreatedAtUtc,
            review.UpdatedAtUtc);
    }

    private static ReviewResponse ToOwnerResponse(Review review)
    {
        return ToResponse(
            review,
            review.Media
                .Where(x => x.DeletedAtUtc == null)
                .OrderBy(x => x.SortOrder)
                .Select(ToMediaResponse)
                .ToArray());
    }

    private static ReviewResponse ToAdminResponse(Review review)
    {
        return ToResponse(
            review,
            review.Media
                .OrderBy(x => x.SortOrder)
                .Select(ToMediaResponse)
                .ToArray());
    }

    private static ReviewResponse ToResponse(Review review, IReadOnlyCollection<ReviewMediaResponse> media)
    {
        return new ReviewResponse(
            review.Id,
            review.ProductId,
            review.VariantId,
            review.OrderId,
            review.OrderItemId,
            review.UserId,
            review.Rating,
            review.Comment,
            review.IsAnonymous,
            review.Status,
            media,
            review.EditedAtUtc,
            review.CreatedAtUtc,
            review.UpdatedAtUtc);
    }

    private static ReviewMediaResponse ToMediaResponse(ReviewMedia media)
    {
        return new ReviewMediaResponse(
            media.Id,
            media.Type,
            ResolveResponseMediaUrl(media),
            NormalizeOptionalUrl(media.ThumbnailUrl),
            media.DurationSec,
            media.FileSizeBytes,
            media.MimeType,
            media.SortOrder,
            media.Status,
            media.CreatedAtUtc,
            media.UpdatedAtUtc);
    }

    private static string ResolveResponseMediaUrl(ReviewMedia media)
    {
        var url = media.Url.Trim();
        return IsAbsoluteHttpUrl(url) || url.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"/api/reviews/media/{media.Id:D}/file";
    }

    private static string? NormalizeOptionalUrl(string? url)
        => string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    private static bool IsAbsoluteHttpUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private sealed record ReviewableOrderItem(Guid OrderId, Guid ProductId, Guid? VariantId);
    private sealed record CustomerPublicProfile(Guid CustomerId, string DisplayName, string? PictureUrl);
}
