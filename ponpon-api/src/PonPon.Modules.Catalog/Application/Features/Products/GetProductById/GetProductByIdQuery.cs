namespace PonPon.Modules.Catalog.Application.Features.Products.GetProductById;

public sealed record GetProductByIdQuery(Guid Id, bool IncludeInactive = false);
