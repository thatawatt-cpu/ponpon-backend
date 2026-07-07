using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Identity.Application.Features.CustomerAddresses;

namespace PonPon.Modules.Identity.Controllers;

[ApiController]
[Route("api/customer-addresses")]
[Authorize]
public sealed class CustomerAddressesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CustomerAddressResponse>>> GetMyAddresses(
        [FromServices] GetMyCustomerAddressesHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerAddressResponse>> CreateAddress(
        [FromBody] CreateCustomerAddressRequest request,
        [FromServices] CreateCustomerAddressHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(
            new CreateCustomerAddressCommand(
                request.RecipientName,
                request.Phone,
                request.Email,
                request.AddressLine1,
                request.AddressLine2,
                request.Subdistrict,
                request.District,
                request.Province,
                request.Postcode,
                request.Country,
                request.Label,
                request.IsDefault),
            cancellationToken);

        return CreatedAtAction(nameof(GetMyAddresses), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerAddressResponse>> UpdateAddress(
        Guid id,
        [FromBody] UpdateCustomerAddressRequest request,
        [FromServices] UpdateCustomerAddressHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new UpdateCustomerAddressCommand(
                id,
                request.RecipientName,
                request.Phone,
                request.Email,
                request.AddressLine1,
                request.AddressLine2,
                request.Subdistrict,
                request.District,
                request.Province,
                request.Postcode,
                request.Country,
                request.Label,
                request.IsDefault),
            cancellationToken));
    }

    [HttpPut("{id:guid}/default")]
    public async Task<ActionResult<CustomerAddressResponse>> SetDefaultAddress(
        Guid id,
        [FromServices] SetDefaultCustomerAddressHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAddress(
        Guid id,
        [FromServices] DeleteCustomerAddressHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
