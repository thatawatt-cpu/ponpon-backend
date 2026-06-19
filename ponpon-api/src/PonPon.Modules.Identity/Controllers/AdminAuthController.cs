using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Identity.Application.Features.AdminLogin;

namespace PonPon.Modules.Identity.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request, [FromServices] AdminLoginHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new AdminLoginCommand(request.Email, request.Password), cancellationToken));
    }
}
