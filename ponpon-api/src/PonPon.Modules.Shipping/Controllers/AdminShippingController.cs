using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Shipping.Application.Features.ShippopSender;

namespace PonPon.Modules.Shipping.Controllers;

[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public sealed class AdminShippingController : ControllerBase
{
    [HttpGet("sender")]
    public async Task<ActionResult<ShippopSenderResponse>> GetSender(
        [FromServices] GetShippopSenderHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpPut("sender")]
    public async Task<ActionResult<ShippopSenderResponse>> UpdateSender(
        [FromBody] UpdateShippopSenderRequest request,
        [FromServices] UpdateShippopSenderHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateShippopSenderCommand(
            request.Name,
            request.Phone,
            request.Email,
            request.Address,
            request.District,
            request.State,
            request.Province,
            request.Postcode);

        return Ok(await handler.HandleAsync(command, cancellationToken));
    }
}
