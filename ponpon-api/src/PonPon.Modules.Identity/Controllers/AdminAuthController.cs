using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Identity.Application.Features.AdminLogin;
using PonPon.Modules.Identity.Application.Features.RegisterFirstAdmin;

namespace PonPon.Modules.Identity.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    [HttpPost("register-first-admin")]
    public async Task<ActionResult<RegisterFirstAdminResponse>> RegisterFirstAdmin(
        [FromBody] RegisterFirstAdminRequest request,
        [FromServices] RegisterFirstAdminHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new RegisterFirstAdminCommand(request.Email, request.Password, request.DisplayName),
            cancellationToken));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        [FromBody] AdminLoginRequest request,
        [FromServices] AdminLoginHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new AdminLoginCommand(request.Email, request.Password),
            cancellationToken));
    }
}
