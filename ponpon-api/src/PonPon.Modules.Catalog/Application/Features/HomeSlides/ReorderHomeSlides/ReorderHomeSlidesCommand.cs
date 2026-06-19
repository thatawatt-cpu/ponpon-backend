namespace PonPon.Modules.Catalog.Application.Features.HomeSlides.ReorderHomeSlides;

public sealed record ReorderHomeSlidesCommand(IReadOnlyList<(Guid Id, int SortOrder)> Slides);
