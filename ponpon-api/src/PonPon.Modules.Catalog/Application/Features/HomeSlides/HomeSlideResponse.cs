using PonPon.Modules.Catalog.Domain.HomeSlides;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides;

public sealed record HomeSlideResponse(
    Guid Id,
    string Image,
    string Badge,
    string Title,
    string Description,
    string LinkUrl,
    string CtaLabel,
    HomeSlideStatus Status,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int SortOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static HomeSlideResponse From(HomeSlide slide)
    {
        return new HomeSlideResponse(
            slide.Id,
            slide.Image,
            slide.Badge,
            slide.Title,
            slide.Description,
            slide.LinkUrl,
            slide.CtaLabel,
            slide.Status,
            slide.StartsAt,
            slide.EndsAt,
            slide.SortOrder,
            slide.CreatedAt,
            slide.UpdatedAt);
    }
}
