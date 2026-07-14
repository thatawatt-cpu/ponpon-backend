using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;
using PonPon.Modules.Catalog.Infrastructure.Persistence;
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
    private const string VideoThumbnailMimeType = "image/jpeg";
    private const string VideoThumbnailExtension = ".jpg";
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
    private readonly CatalogDbContext _catalog;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDateTimeProvider _clock;
    private readonly ISupabaseStorageService _storage;

    public ReviewService(
        ReviewsDbContext reviews,
        OrderingDbContext ordering,
        IdentityDbContext identity,
        CatalogDbContext catalog,
        IHttpClientFactory httpClientFactory,
        IDateTimeProvider clock,
        ISupabaseStorageService storage)
    {
        _reviews = reviews;
        _ordering = ordering;
        _identity = identity;
        _catalog = catalog;
        _httpClientFactory = httpClientFactory;
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
        var customerIds = reviews
            .Select(x => x.UserId)
            .Distinct()
            .ToArray();
        var customers = await _identity.Customers
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .Select(x => new AdminReviewListCustomerResponse(
                x.Id,
                x.LineProfile.DisplayName,
                x.LineProfile.PictureUrl))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var productIds = reviews
            .Select(x => x.ProductId)
            .Distinct()
            .ToArray();
        var products = await _catalog.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .Select(x => new AdminReviewListProductResponse(
                x.Id,
                x.Name,
                x.ImageUrl))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return new AdminReviewListResponse(
            reviews.Select(x => ToAdminListItemResponse(
                x,
                customers.GetValueOrDefault(x.UserId),
                products.GetValueOrDefault(x.ProductId))).ToArray(),
            total,
            page,
            pageSize);
    }

    public async Task<AdminReviewDetailResponse> GetAdminReviewByIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var review = await _reviews.Reviews
            .AsNoTracking()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Review was not found.");

        var customer = await _identity.Customers
            .AsNoTracking()
            .Where(x => x.Id == review.UserId)
            .Select(x => new AdminReviewCustomerResponse(
                x.Id,
                x.LineProfile.DisplayName,
                x.LineProfile.LineUserId,
                x.LineProfile.PictureUrl,
                x.LineProfile.Email))
            .FirstOrDefaultAsync(cancellationToken);

        var product = await _catalog.Products
            .AsNoTracking()
            .Where(x => x.Id == review.ProductId)
            .Select(x => new AdminReviewProductResponse(
                x.Id,
                x.Name,
                x.Slug,
                x.ImageUrl))
            .FirstOrDefaultAsync(cancellationToken);

        var order = await _ordering.Orders
            .AsNoTracking()
            .Where(x => x.Id == review.OrderId)
            .Select(x => new AdminReviewOrderResponse(
                x.Id,
                x.Number,
                x.Status,
                x.PaymentStatus,
                x.OrderDate,
                x.CustomerName,
                x.CustomerPhone))
            .FirstOrDefaultAsync(cancellationToken);

        var orderItem = await _ordering.OrderItems
            .AsNoTracking()
            .Where(x => x.Id == review.OrderItemId)
            .Select(x => new AdminReviewOrderItemResponse(
                x.Id,
                x.ProductId,
                x.VariantId,
                x.Sku,
                x.Name,
                x.Quantity,
                x.PricePerUnit,
                x.TotalPrice,
                x.ImageUrl))
            .FirstOrDefaultAsync(cancellationToken);

        return ToAdminDetailResponse(review, customer, product, order, orderItem);
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
                var thumbnail = await TryGenerateAndUploadVideoThumbnailAsync(
                    upload,
                    BuildReviewVideoThumbnailPath(orderItemId, upload.FileName),
                    cancellationToken);
                if (thumbnail is not null)
                {
                    uploadedPaths.Add(thumbnail.Path);
                }

                var path = BuildReviewMediaPath(orderItemId, upload.FileName);
                var url = await _storage.UploadAsync(path, upload.FileStream, upload.MimeType, cancellationToken);
                uploadedPaths.Add(path);
                media.Add(new CreateReviewMediaRequest(
                    upload.Type,
                    url,
                    thumbnail?.Url,
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
                var thumbnail = await TryGenerateAndUploadVideoThumbnailAsync(
                    upload,
                    BuildReviewVideoThumbnailPath(review.OrderItemId, upload.FileName),
                    cancellationToken);
                if (thumbnail is not null)
                {
                    uploadedPaths.Add(thumbnail.Path);
                }

                var path = BuildReviewMediaPath(review.OrderItemId, upload.FileName);
                var url = await _storage.UploadAsync(path, upload.FileStream, upload.MimeType, cancellationToken);
                uploadedPaths.Add(path);
                media.Add(new CreateReviewMediaRequest(
                    upload.Type,
                    url,
                    thumbnail?.Url,
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
        var normalized = NormalizeReviewStatus(status);

        var review = await _reviews.Reviews
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Review was not found.");

        if (review.DeletedAtUtc is not null)
        {
            throw new BadRequestException("Deleted reviews cannot be published or hidden.");
        }

        var now = _clock.UtcNow;
        ApplyReviewStatus(review, normalized, now);

        await _reviews.SaveChangesAsync(cancellationToken);
        return ToAdminResponse(review);
    }

    public async Task<BulkUpdateReviewStatusResponse> UpdateReviewStatusesAsync(
        IReadOnlyCollection<Guid> reviewIds,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (reviewIds is null || reviewIds.Count == 0)
        {
            throw new BadRequestException("At least one review id is required.");
        }

        var normalized = NormalizeReviewStatus(status);
        var distinctIds = reviewIds.Distinct().ToArray();
        var reviews = await _reviews.Reviews
            .Where(x => distinctIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        if (reviews.Length != distinctIds.Length)
        {
            throw new NotFoundException("One or more reviews were not found.");
        }

        if (reviews.Any(x => x.DeletedAtUtc is not null))
        {
            throw new BadRequestException("Deleted reviews cannot be published or hidden.");
        }

        var now = _clock.UtcNow;
        foreach (var review in reviews)
        {
            ApplyReviewStatus(review, normalized, now);
        }

        await _reviews.SaveChangesAsync(cancellationToken);
        return new BulkUpdateReviewStatusResponse(reviews.Length, normalized);
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

            var completedUrl = request.Url ?? media.Url;
            var thumbnailUrl = request.ThumbnailUrl;
            if (media.Type == ReviewMediaType.Video && string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                thumbnailUrl = await TryGenerateAndUploadVideoThumbnailAsync(
                    completedUrl,
                    BuildReviewVideoThumbnailPath(media.Id),
                    cancellationToken);
            }

            media.Complete(completedUrl, thumbnailUrl, request.DurationSec, now);
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

    private static string BuildReviewVideoThumbnailPath(Guid orderItemId, string fileName)
        => $"reviews/{orderItemId:D}/{Guid.NewGuid():D}{VideoThumbnailExtension}";

    private static string BuildReviewVideoThumbnailPath(Guid mediaId)
        => $"reviews/{mediaId:D}/thumbnail{VideoThumbnailExtension}";

    private async Task<UploadedVideoThumbnail?> TryGenerateAndUploadVideoThumbnailAsync(
        CreateReviewUploadMediaRequest upload,
        string thumbnailPath,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(upload.Type, ReviewMediaType.Video, StringComparison.OrdinalIgnoreCase)
            || !upload.FileStream.CanSeek)
        {
            return null;
        }

        var originalPosition = upload.FileStream.Position;
        var videoFile = BuildTempFilePath(Path.GetExtension(upload.FileName));
        var thumbnailFile = BuildTempFilePath(VideoThumbnailExtension);
        try
        {
            upload.FileStream.Position = 0;
            await using (var output = File.Create(videoFile))
            {
                await upload.FileStream.CopyToAsync(output, cancellationToken);
            }

            upload.FileStream.Position = 0;
            if (!await TryCreateVideoThumbnailFileAsync(videoFile, thumbnailFile, cancellationToken))
            {
                return null;
            }

            await using var thumbnailStream = File.OpenRead(thumbnailFile);
            var url = await _storage.UploadAsync(
                thumbnailPath,
                thumbnailStream,
                VideoThumbnailMimeType,
                cancellationToken);

            return new UploadedVideoThumbnail(url, thumbnailPath);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (upload.FileStream.CanSeek)
            {
                upload.FileStream.Position = originalPosition;
            }

            DeleteTempFile(videoFile);
            DeleteTempFile(thumbnailFile);
        }
    }

    private async Task<string?> TryGenerateAndUploadVideoThumbnailAsync(
        string videoUrlOrPath,
        string thumbnailPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(videoUrlOrPath))
        {
            return null;
        }

        var videoFile = BuildTempFilePath(Path.GetExtension(videoUrlOrPath));
        var thumbnailFile = BuildTempFilePath(VideoThumbnailExtension);
        try
        {
            var sourceUrl = videoUrlOrPath.Trim();
            if (!IsAbsoluteHttpUrl(sourceUrl))
            {
                sourceUrl = await _storage.GetPublicUrlAsync(sourceUrl, cancellationToken);
            }

            using var response = await _httpClientFactory
                .CreateClient()
                .GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = File.Create(videoFile))
            {
                await input.CopyToAsync(output, cancellationToken);
            }

            if (!await TryCreateVideoThumbnailFileAsync(videoFile, thumbnailFile, cancellationToken))
            {
                return null;
            }

            await using var thumbnailStream = File.OpenRead(thumbnailFile);
            return await _storage.UploadAsync(
                thumbnailPath,
                thumbnailStream,
                VideoThumbnailMimeType,
                cancellationToken);
        }
        catch
        {
            return null;
        }
        finally
        {
            DeleteTempFile(videoFile);
            DeleteTempFile(thumbnailFile);
        }
    }

    private static async Task<bool> TryCreateVideoThumbnailFileAsync(
        string videoFile,
        string thumbnailFile,
        CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add("-hide_banner");
            startInfo.ArgumentList.Add("-loglevel");
            startInfo.ArgumentList.Add("error");
            startInfo.ArgumentList.Add("-ss");
            startInfo.ArgumentList.Add("00:00:01");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(videoFile);
            startInfo.ArgumentList.Add("-frames:v");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-q:v");
            startInfo.ArgumentList.Add("3");
            startInfo.ArgumentList.Add(thumbnailFile);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0
                   && File.Exists(thumbnailFile)
                   && new FileInfo(thumbnailFile).Length > 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildTempFilePath(string? extension)
    {
        extension = string.IsNullOrWhiteSpace(extension) ? ".tmp" : extension;
        extension = extension.Split('?', '#')[0];
        if (!extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        if (extension.Length > 12 || extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            extension = ".tmp";
        }

        return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():D}{extension}");
    }

    private static void DeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temp cleanup is best effort; thumbnail generation must not block review submission.
        }
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

    private static AdminReviewListItemResponse ToAdminListItemResponse(
        Review review,
        AdminReviewListCustomerResponse? customer,
        AdminReviewListProductResponse? product)
    {
        return new AdminReviewListItemResponse(
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
            customer,
            product,
            review.Media
                .OrderBy(x => x.SortOrder)
                .Select(ToMediaResponse)
                .ToArray(),
            review.EditedAtUtc,
            review.CreatedAtUtc,
            review.UpdatedAtUtc);
    }

    private static AdminReviewDetailResponse ToAdminDetailResponse(
        Review review,
        AdminReviewCustomerResponse? customer,
        AdminReviewProductResponse? product,
        AdminReviewOrderResponse? order,
        AdminReviewOrderItemResponse? orderItem)
    {
        var media = review.Media
            .OrderBy(x => x.SortOrder)
            .Select(ToMediaResponse)
            .ToArray();

        return new AdminReviewDetailResponse(
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
            customer,
            product,
            order,
            orderItem,
            orderItem?.Sku,
            media,
            [],
            review.EditedAtUtc,
            review.CreatedAtUtc,
            review.UpdatedAtUtc,
            review.DeletedAtUtc);
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

    private static string NormalizeReviewStatus(string status)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!ReviewStatus.IsValid(normalized))
        {
            throw new BadRequestException("Review status must be published or hidden.");
        }

        return normalized;
    }

    private static void ApplyReviewStatus(Review review, string status, DateTime now)
    {
        if (status == ReviewStatus.Published)
        {
            review.Publish(now);
            return;
        }

        review.Hide(now);
    }

    private sealed record ReviewableOrderItem(Guid OrderId, Guid ProductId, Guid? VariantId);
    private sealed record CustomerPublicProfile(Guid CustomerId, string DisplayName, string? PictureUrl);
    private sealed record UploadedVideoThumbnail(string Url, string Path);
}
