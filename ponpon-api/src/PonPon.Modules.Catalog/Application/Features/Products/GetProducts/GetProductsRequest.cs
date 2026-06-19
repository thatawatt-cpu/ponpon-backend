namespace PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

public sealed record GetProductsRequest(string? Keyword, string? Category, int Page = 1, int PageSize = 20);
