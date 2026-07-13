using PonPon.Shared.Domain;

namespace PonPon.Modules.Reviews.Domain;

public sealed class ReviewMedia : Entity, IAuditableEntity
{
    private ReviewMedia()
    {
        Type = string.Empty;
        Url = string.Empty;
        MimeType = string.Empty;
        Status = ReviewMediaStatus.Processing;
    }

    private ReviewMedia(
        Guid reviewId,
        string type,
        string url,
        string? thumbnailUrl,
        int? durationSec,
        long? fileSizeBytes,
        string? mimeType,
        int sortOrder,
        string status,
        DateTime now) : this()
    {
        ReviewId = reviewId;
        Type = type;
        Url = url.Trim();
        ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl.Trim();
        DurationSec = durationSec;
        FileSizeBytes = fileSizeBytes;
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? null : mimeType.Trim();
        SortOrder = sortOrder;
        Status = status;
        CreatedAtUtc = now;
    }

    public Guid ReviewId { get; private set; }
    public string Type { get; private set; }
    public string Url { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public int? DurationSec { get; private set; }
    public long? FileSizeBytes { get; private set; }
    public string? MimeType { get; private set; }
    public int SortOrder { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public static ReviewMedia Create(
        Guid reviewId,
        string type,
        string url,
        string? thumbnailUrl,
        int? durationSec,
        long? fileSizeBytes,
        string? mimeType,
        int sortOrder,
        DateTime now)
    {
        var normalizedType = type.Trim().ToLowerInvariant();
        return new ReviewMedia(
            reviewId,
            normalizedType,
            url,
            thumbnailUrl,
            durationSec,
            fileSizeBytes,
            mimeType,
            sortOrder,
            ReviewMediaStatus.Ready,
            now);
    }

    public static ReviewMedia CreatePendingUpload(
        Guid reviewId,
        string type,
        string url,
        string? thumbnailUrl,
        int? durationSec,
        long? fileSizeBytes,
        string? mimeType,
        int sortOrder,
        DateTime now)
    {
        return new ReviewMedia(
            reviewId,
            type.Trim().ToLowerInvariant(),
            url,
            thumbnailUrl,
            durationSec,
            fileSizeBytes,
            mimeType,
            sortOrder,
            ReviewMediaStatus.Processing,
            now);
    }

    public void Complete(string url, string? thumbnailUrl, int? durationSec, DateTime now)
    {
        Url = string.IsNullOrWhiteSpace(url) ? Url : url.Trim();
        ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl) ? ThumbnailUrl : thumbnailUrl.Trim();
        DurationSec = durationSec ?? DurationSec;
        Status = ReviewMediaStatus.Ready;
        UpdatedAtUtc = now;
    }

    public void Fail(DateTime now)
    {
        Status = ReviewMediaStatus.Failed;
        UpdatedAtUtc = now;
    }

    public void SoftDelete(DateTime now)
    {
        DeletedAtUtc = now;
        UpdatedAtUtc = now;
    }
}
