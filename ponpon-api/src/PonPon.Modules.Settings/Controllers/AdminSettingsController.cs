using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Settings.Application.Features.IntegrationSettings;
using PonPon.Modules.Settings.Application.Features.GetZortWebhookFromZort;
using PonPon.Modules.Settings.Application.Features.RegisterZortWebhook;

namespace PonPon.Modules.Settings.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = "Admin")]
public sealed class AdminSettingsController : ControllerBase
{
    [HttpGet("integrations")]
    public async Task<ActionResult<IntegrationSettingsResponse>> GetIntegrations(
        [FromServices] GetIntegrationSettingsHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(cancellationToken));

    [HttpPut("integrations/{group}")]
    public async Task<IActionResult> UpdateIntegration(
        string group,
        [FromBody] UpdateIntegrationSettingsRequest request,
        [FromServices] UpdateIntegrationSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(group, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("integrations/ZORT/webhooks")]
    public async Task<IActionResult> GetZortWebhookFromZort(
        [FromServices] GetZortWebhookFromZortHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("integrations/ZORT/register-webhook")]
    public async Task<IActionResult> RegisterZortWebhook(
        [FromServices] RegisterZortWebhookHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(cancellationToken);
        return NoContent();
    }
}
