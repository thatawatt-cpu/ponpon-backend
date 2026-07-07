using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductById;
using PonPon.Modules.Catalog.Application.Features.Products.GetProducts;
using PonPon.Modules.Catalog.Application.Features.Products.SyncProductsFromZort;
using PonPon.Modules.Catalog.Application.Features.Products.UploadProductImage;
using PonPon.Modules.Catalog.Application.Features.Products.UpdateProductImages;
using PonPon.Modules.Catalog.Application.Features.Products.UpdateProductPonPonSettings;
using PonPon.Modules.Catalog.Application.Features.Products.UpdateProductVisibility;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Domain.SyncRuns;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public sealed class AdminProductsController : ControllerBase
{
    [HttpPost("sync-zort")]
    public async Task<ActionResult<SyncQueuedResponse>> SyncZort(
        [FromBody] SyncProductsFromZortRequest request,
        [FromServices] IProductSyncRunRepository syncRuns,
        [FromServices] ICatalogUnitOfWork unitOfWork,
        [FromServices] IPersistentBackgroundJobClient backgroundJobs,
        [FromServices] IDateTimeProvider clock,
        CancellationToken cancellationToken)
    {
        var syncRun = ProductSyncRun.Queue(clock.UtcNow);
        await syncRuns.AddAsync(syncRun, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var backgroundJobId = backgroundJobs.Enqueue<ProductSyncBackgroundJob>(job =>
                job.ExecuteAsync(
                    syncRun.Id,
                    request.PageStart,
                    request.PageLimit,
                    request.MaxPages,
                    request.DeactivateMissingProducts));

            syncRun.AttachBackgroundJob(backgroundJobId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Accepted(new SyncQueuedResponse(syncRun.Id, backgroundJobId, syncRun.Status));
        }
        catch (Exception ex)
        {
            syncRun.MarkFailed(ex.Message, clock.UtcNow);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProductListItemResponse>>> GetProducts([FromQuery] string? keyword, [FromQuery] ProductStatus? status, [FromQuery] ProductSource? source, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] GetProductsHandler handler = null!, CancellationToken cancellationToken = default)
    {
        return Ok(await handler.HandleAsync(new GetProductsQuery(keyword, null, status, source, page, pageSize, IncludeInactive: true), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> GetProductById(Guid id, [FromServices] GetProductByIdHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetProductByIdQuery(id, IncludeInactive: true), cancellationToken));
    }

    [HttpPatch("{id:guid}/visibility")]
    public async Task<IActionResult> UpdateVisibility(Guid id, [FromBody] UpdateProductVisibilityRequest request, [FromServices] UpdateProductVisibilityHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new UpdateProductVisibilityCommand(id, request.IsVisibleOnLiff), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/images/upload")]
    public async Task<ActionResult<UploadProductImageResponse>> UploadImage(Guid id, IFormFile file, [FromServices] UploadProductImageHandler handler, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(new UploadProductImageCommand(id, stream, file.FileName, file.ContentType), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/images")]
    public async Task<IActionResult> UpdateImages(Guid id, [FromBody] UpdateProductImagesRequest request, [FromServices] UpdateProductImagesHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new UpdateProductImagesCommand(
            id,
            request.Images.Select(x => (x.Url, x.SortOrder, x.IsPrimary)).ToArray()), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/ponpon-settings")]
    public async Task<IActionResult> UpdatePonPonSettings(Guid id, [FromBody] UpdateProductPonPonSettingsRequest request, [FromServices] UpdateProductPonPonSettingsHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new UpdateProductPonPonSettingsCommand(
            id,
            request.Slug,
            request.OriginalPrice,
            request.PromotionBadge,
            request.Highlights,
            request.RichDescription,
            request.IsFeatured,
            request.IsBestSeller,
            request.IsOnHomepage,
            request.Options?.Select(o => new ProductVariantOptionCommand(o.Name, o.Value)).ToArray()), cancellationToken);
        return NoContent();
    }
}

public sealed record SyncQueuedResponse(Guid SyncRunId, string BackgroundJobId, string Status);
