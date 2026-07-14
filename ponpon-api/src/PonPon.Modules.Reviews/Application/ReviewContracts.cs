using Microsoft.AspNetCore.Http;

namespace PonPon.Modules.Reviews.Application;

public sealed class CreateReviewFormRequest
{
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public bool IsAnonymous { get; init; }
    public List<CreateReviewFormMediaRequest>? Media { get; init; }
}

public sealed class CreateReviewFormMediaRequest
{
    public string? Type { get; init; }
    public IFormFile? File { get; init; }
    public int? DurationSec { get; init; }
    public int SortOrder { get; init; }
}

public sealed record CreateReviewUploadMediaRequest(
    string Type,
    Stream FileStream,
    string FileName,
    string MimeType,
    long FileSizeBytes,
    int? DurationSec,
    int SortOrder);

public sealed class UpdateReviewFormRequest
{
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public bool IsAnonymous { get; init; }
    public List<CreateReviewFormMediaRequest>? Media { get; init; }
}

public sealed record CreateReviewMediaRequest(
    string Type,
    string Url,
    string? ThumbnailUrl,
    int? DurationSec,
    long? FileSizeBytes,
    string? MimeType,
    int SortOrder);

public sealed record ReviewResponse(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    Guid OrderId,
    Guid OrderItemId,
    Guid UserId,
    int Rating,
    string Comment,
    bool IsAnonymous,
    string Status,
    IReadOnlyCollection<ReviewMediaResponse> Media,
    DateTime? EditedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PublicReviewResponse(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    int Rating,
    string Comment,
    bool IsAnonymous,
    string? UserName,
    string? UserAvatar,
    IReadOnlyCollection<ReviewMediaResponse> Media,
    DateTime? EditedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record ReviewMediaResponse(
    Guid Id,
    string Type,
    string Url,
    string? ThumbnailUrl,
    int? DurationSec,
    long? FileSizeBytes,
    string? MimeType,
    int SortOrder,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record ProductReviewSummaryResponse(
    Guid ProductId,
    int TotalReviews,
    decimal AverageRating,
    int Rating1Count,
    int Rating2Count,
    int Rating3Count,
    int Rating4Count,
    int Rating5Count);

public sealed record AdminReviewListResponse(
    IReadOnlyCollection<AdminReviewListItemResponse> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AdminReviewListItemResponse(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    Guid OrderId,
    Guid OrderItemId,
    Guid UserId,
    int Rating,
    string Comment,
    bool IsAnonymous,
    string Status,
    AdminReviewListCustomerResponse? Customer,
    AdminReviewListProductResponse? Product,
    IReadOnlyCollection<ReviewMediaResponse> Media,
    DateTime? EditedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record AdminReviewListCustomerResponse(
    Guid Id,
    string DisplayName,
    string? PictureUrl);

public sealed record AdminReviewListProductResponse(
    Guid Id,
    string Name,
    string? Slug,
    string? ImageUrl);

public sealed record UpdateReviewStatusRequest(string Status);

public sealed record BulkUpdateReviewStatusRequest(
    IReadOnlyCollection<Guid> ReviewIds,
    string Status);

public sealed record BulkUpdateReviewStatusResponse(
    int UpdatedCount,
    string Status);

public sealed record AdminReviewDetailResponse(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    Guid OrderId,
    Guid OrderItemId,
    Guid UserId,
    int Rating,
    string Comment,
    bool IsAnonymous,
    string Status,
    AdminReviewCustomerResponse? Customer,
    AdminReviewProductResponse? Product,
    AdminReviewOrderResponse? Order,
    AdminReviewOrderItemResponse? OrderItem,
    string? Sku,
    IReadOnlyCollection<ReviewMediaResponse> Media,
    IReadOnlyCollection<AdminReviewActionHistoryItemResponse> ActionHistory,
    DateTime? EditedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? DeletedAtUtc);

public sealed record AdminReviewCustomerResponse(
    Guid CustomerId,
    string DisplayName,
    string LineUserId,
    string? PictureUrl,
    string? Email);

public sealed record AdminReviewProductResponse(
    Guid ProductId,
    string Name,
    string? Slug,
    string? ImageUrl);

public sealed record AdminReviewOrderResponse(
    Guid OrderId,
    string Number,
    string Status,
    string PaymentStatus,
    DateTime? OrderDate,
    string? CustomerName,
    string? CustomerPhone);

public sealed record AdminReviewOrderItemResponse(
    Guid OrderItemId,
    Guid? ProductId,
    Guid? VariantId,
    string Sku,
    string Name,
    decimal Quantity,
    decimal PricePerUnit,
    decimal TotalPrice,
    string? ImageUrl);

public sealed record AdminReviewActionHistoryItemResponse(
    DateTime CreatedAtUtc,
    string Action,
    string? Actor,
    string? Note);

public sealed record ReviewMediaUploadUrlRequest(
    Guid ReviewId,
    string Type,
    string FileName,
    string MimeType,
    long? FileSizeBytes,
    int? DurationSec,
    int SortOrder);

public sealed record ReviewMediaUploadUrlResponse(
    Guid MediaId,
    string UploadUrl,
    string PublicUrl,
    string Status);

public sealed record CompleteReviewMediaRequest(
    string? Url,
    string? ThumbnailUrl,
    int? DurationSec,
    bool Failed);
