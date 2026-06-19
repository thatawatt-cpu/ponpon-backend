using PonPon.Modules.Catalog.Domain.HomeSlides;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.CreateHomeSlide;

public sealed record CreateHomeSlideCommand(
    string Image,
    string? Badge,
    string Title,
    string? Description,
    string LinkUrl,
    string? CtaLabel,
    HomeSlideStatus Status,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int SortOrder);
