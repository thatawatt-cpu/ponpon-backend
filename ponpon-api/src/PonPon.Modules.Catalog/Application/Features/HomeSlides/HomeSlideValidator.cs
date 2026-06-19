using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.HomeSlides;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.HomeSlides;

internal static class HomeSlideValidator
{
    public const int MaxSlides = 10;

    public static void ValidateFields(string image, string title, string linkUrl, HomeSlideStatus status, DateTime? startsAt, DateTime? endsAt, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(image))
            throw new BadRequestException("Image is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new BadRequestException("Title is required.");

        if (string.IsNullOrWhiteSpace(linkUrl))
            throw new BadRequestException("Link URL is required.");

        if (!Enum.IsDefined(status))
            throw new BadRequestException("Status is invalid.");

        if (sortOrder < 1)
            throw new BadRequestException("Sort order must be greater than zero.");

        if (startsAt.HasValue && endsAt.HasValue && startsAt.Value >= endsAt.Value)
            throw new BadRequestException("StartsAt must be earlier than EndsAt.");
    }

    public static async Task EnsureSortOrderAvailableAsync(IHomeSlideRepository slides, int sortOrder, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await slides.SortOrderExistsAsync(sortOrder, exceptId, cancellationToken))
            throw new BadRequestException("Sort order already exists.");
    }

    public static async Task EnsureActiveQuotaAvailableAsync(IHomeSlideRepository slides, HomeSlideStatus status, DateTime? endsAt, DateTime now, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (status == HomeSlideStatus.Inactive || (endsAt.HasValue && endsAt.Value <= now))
            return;

        if (await slides.CountActiveQuotaAsync(now, exceptId, cancellationToken) >= MaxSlides)
            throw new BadRequestException("Active home slides cannot exceed 10 slides.");
    }
}
