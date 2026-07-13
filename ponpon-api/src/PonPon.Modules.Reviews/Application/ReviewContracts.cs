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
    IReadOnlyCollection<ReviewResponse> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record UpdateReviewStatusRequest(string Status);

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
