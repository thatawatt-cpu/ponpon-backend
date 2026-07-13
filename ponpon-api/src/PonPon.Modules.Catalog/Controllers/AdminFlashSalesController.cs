using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PonPon.Modules.Catalog.Application.Features.FlashSales.CreateFlashSale;
using PonPon.Modules.Catalog.Application.Features.FlashSales.DeleteFlashSale;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSaleById;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;
using PonPon.Modules.Catalog.Application.Features.FlashSales.UpdateFlashSale;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
public sealed class AdminFlashSalesController : ControllerBase
{
    [HttpGet("api/admin/flash-sales")]
    public async Task<ActionResult<IReadOnlyCollection<FlashSaleResponse>>> GetAll([FromServices] GetFlashSalesHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpGet("api/admin/flash-sales/{id:guid}")]
    public async Task<ActionResult<FlashSaleResponse>> GetById(Guid id, [FromServices] GetFlashSaleByIdHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetFlashSaleByIdQuery(id), cancellationToken));
    }

    [HttpPost("api/admin/flash-sales")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateFlashSaleRequest request, [FromServices] CreateFlashSaleHandler handler, CancellationToken cancellationToken)
    {
        var command = new CreateFlashSaleCommand(
            request.Name,
            request.StartDate,
            request.EndDate,
            request.IsActive,
            request.Slots,
            request.Products.Select(p => (p.ProductId, p.SalePrice, p.QuantityLimit)).ToArray());

        var id = await handler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("api/admin/flash-sales/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateFlashSaleRequest request, [FromServices] UpdateFlashSaleHandler handler, CancellationToken cancellationToken)
    {
        var command = new UpdateFlashSaleCommand(
            id,
            request.Name,
            request.StartDate,
            request.EndDate,
            request.IsActive,
            request.Slots,
            request.Products.Select(p => (p.ProductId, p.SalePrice, p.QuantityLimit)).ToArray());

        await handler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("api/admin/flash-sales/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] DeleteFlashSaleHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteFlashSaleCommand(id), cancellationToken);
        return NoContent();
    }
}
