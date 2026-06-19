namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.ReorderHomeSlides;

public sealed record ReorderHomeSlidesRequest(IReadOnlyList<ReorderHomeSlideItem> Slides);

public sealed record ReorderHomeSlideItem(Guid Id, int SortOrder);
