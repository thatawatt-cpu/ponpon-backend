namespace PonPon.Modules.Catalog.Application.Features.Products.SyncProductsFromZort;

public sealed record SyncProductsFromZortRequest(int PageStart = 1, int PageLimit = 100, int? MaxPages = null, bool DeactivateMissingProducts = false);
