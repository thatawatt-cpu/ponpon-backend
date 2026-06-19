namespace PonPon.Modules.Catalog.Application.Features.Products.SyncProductsFromZort;

public sealed record SyncProductsFromZortResponse(int TotalFetched, int Created, int Updated, int Unchanged, int Deactivated, int Failed, IReadOnlyCollection<string> Errors);
