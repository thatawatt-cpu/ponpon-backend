namespace PonPon.Modules.Catalog.Domain.HomeSlides;

public sealed class HomeSlide
{
    private HomeSlide()
    {
        Image = string.Empty;
        Badge = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
        LinkUrl = string.Empty;
        CtaLabel = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Image { get; private set; }
    public string Badge { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string LinkUrl { get; private set; }
    public string CtaLabel { get; private set; }
    public HomeSlideStatus Status { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static HomeSlide Create(
        string image,
        string badge,
        string title,
        string description,
        string linkUrl,
        string ctaLabel,
        HomeSlideStatus status,
        DateTime? startsAt,
        DateTime? endsAt,
        int sortOrder,
        DateTime now)
    {
        return new HomeSlide
        {
            Id = Guid.NewGuid(),
            Image = image,
            Badge = badge,
            Title = title,
            Description = description,
            LinkUrl = linkUrl,
            CtaLabel = ctaLabel,
            Status = status,
            StartsAt = startsAt,
            EndsAt = endsAt,
            SortOrder = sortOrder,
            CreatedAt = now
        };
    }

    public void Update(
        string image,
        string badge,
        string title,
        string description,
        string linkUrl,
        string ctaLabel,
        HomeSlideStatus status,
        DateTime? startsAt,
        DateTime? endsAt,
        int sortOrder,
        DateTime now)
    {
        Image = image;
        Badge = badge;
        Title = title;
        Description = description;
        LinkUrl = linkUrl;
        CtaLabel = ctaLabel;
        Status = status;
        StartsAt = startsAt;
        EndsAt = endsAt;
        SortOrder = sortOrder;
        UpdatedAt = now;
    }

    public void SetSortOrder(int sortOrder, DateTime now)
    {
        SortOrder = sortOrder;
        UpdatedAt = now;
    }
}
