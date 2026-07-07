using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Features.Warehouses.GetWarehouses;
using PonPon.Modules.Catalog.Application.Features.Warehouses.SyncWarehouses;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[Route("api/admin/warehouses")]
[Authorize(Roles = "Admin")]
public sealed class AdminWarehousesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WarehouseResponse>>> GetWarehouses(
        [FromServices] GetWarehousesHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpPost("sync")]
    public async Task<ActionResult<SyncWarehousesResponse>> SyncWarehouses(
        [FromServices] SyncWarehousesHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new SyncWarehousesCommand(), cancellationToken));
    }
}
