using PonPon.Shared.Domain;

namespace PonPon.Modules.Reviews.Domain;

public sealed class Review : AggregateRoot, IAuditableEntity
{
    private readonly List<ReviewMedia> _media = [];

    private Review()
    {
        Comment = string.Empty;
        Status = ReviewStatus.Published;
    }

    private Review(
        Guid productId,
        Guid? variantId,
        Guid orderId,
        Guid orderItemId,
        Guid userId,
        int rating,
        string comment,
        bool isAnonymous,
        DateTime now) : this()
    {
        ProductId = productId;
        VariantId = variantId;
        OrderId = orderId;
        OrderItemId = orderItemId;
        UserId = userId;
        Rating = rating;
        Comment = comment.Trim();
        IsAnonymous = isAnonymous;
        CreatedAtUtc = now;
    }

    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid UserId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; }
    public bool IsAnonymous { get; private set; }
    public string Status { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public IReadOnlyCollection<ReviewMedia> Media => _media.AsReadOnly();

    public static Review Create(
        Guid productId,
        Guid? variantId,
        Guid orderId,
        Guid orderItemId,
        Guid userId,
        int rating,
        string comment,
        bool isAnonymous,
        DateTime now)
    {
        return new Review(productId, variantId, orderId, orderItemId, userId, rating, comment, isAnonymous, now);
    }

    public void Edit(int rating, string comment, bool isAnonymous, DateTime now)
    {
        Rating = rating;
        Comment = comment.Trim();
        IsAnonymous = isAnonymous;
        EditedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Hide(DateTime now)
    {
        Status = ReviewStatus.Hidden;
        UpdatedAtUtc = now;
    }

    public void Publish(DateTime now)
    {
        Status = ReviewStatus.Published;
        UpdatedAtUtc = now;
    }

    public void SoftDelete(DateTime now)
    {
        DeletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void AddMedia(ReviewMedia media)
    {
        _media.Add(media);
    }
}
