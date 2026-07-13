using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Features.Categories.GetCategories;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductById;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductBySlug;
using PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
public sealed class ProductsController : ControllerBase
{
    [HttpGet("api/products")]
    public async Task<ActionResult<IReadOnlyCollection<ProductListItemResponse>>> GetProducts([FromQuery] GetProductsRequest request, [FromServices] GetProductsHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetProductsQuery(request.Keyword, request.Category, null, null, request.Page, request.PageSize), cancellationToken));
    }

    [HttpGet("api/shop/products")]
    public async Task<ActionResult<IReadOnlyCollection<ProductListItemResponse>>> GetShopProducts([FromQuery] GetProductsRequest request, [FromServices] GetProductsHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetProductsQuery(request.Keyword, request.Category, null, null, request.Page, request.PageSize), cancellationToken));
    }

    [HttpGet("api/products/{id:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> GetProductById(Guid id, [FromServices] GetProductByIdHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetProductByIdQuery(id), cancellationToken));
    }

    [HttpGet("api/products/slug/{slug}")]
    public async Task<ActionResult<ProductDetailResponse>> GetProductBySlug(string slug, [FromServices] GetProductBySlugHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetProductBySlugQuery(slug), cancellationToken));
    }

    [HttpGet("api/categories")]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetCategories([FromServices] GetCategoriesHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetCategoriesQuery(), cancellationToken));
    }
}
