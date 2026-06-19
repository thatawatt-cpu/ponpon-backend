namespace PonPon.Modules.Catalog.Application.Features.Products.UpdateProductVisibility;

public sealed record UpdateProductVisibilityCommand(Guid ProductId, bool IsVisibleOnLiff);
