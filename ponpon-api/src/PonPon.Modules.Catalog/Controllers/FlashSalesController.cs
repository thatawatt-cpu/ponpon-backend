using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetActiveFlashSale;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[Route("api/flash-sales")]
public sealed class FlashSalesController : ControllerBase
{
    [HttpGet("active")]
    public async Task<ActionResult<FlashSaleResponse>> GetActive([FromServices] GetActiveFlashSaleHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        if (result is null)
            return NoContent();

        return Ok(result);
    }
}
