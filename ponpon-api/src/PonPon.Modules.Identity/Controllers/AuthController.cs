using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Identity.Application.Features.GetMe;
using PonPon.Modules.Identity.Application.Features.LineLogin;
using PonPon.Modules.Identity.Application.Features.Logout;
using PonPon.Modules.Identity.Application.Features.RefreshToken;

namespace PonPon.Modules.Identity.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("line-login")]
    public async Task<ActionResult<LineLoginResponse>> LineLogin([FromBody] LineLoginRequest request, [FromServices] LineLoginHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new LineLoginCommand(request.IdToken), cancellationToken));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request, [FromServices] RefreshTokenHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new RefreshTokenCommand(request.RefreshToken), cancellationToken));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me([FromServices] GetMeHandler handler, CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new GetMeQuery(), cancellationToken));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, [FromServices] LogoutHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new LogoutCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }
}
