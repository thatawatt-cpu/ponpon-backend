using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Settings.Application.Features.IntegrationSettings;
using PonPon.Modules.Settings.Application.Features.GetZortWebhook;
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

    [HttpGet("zort/webhook")]
    public async Task<IActionResult> GetZortWebhook(
        [FromServices] GetZortWebhookHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("zort/webhook-from-zort")]
    public async Task<IActionResult> GetZortWebhookFromZort(
        [FromServices] GetZortWebhookFromZortHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("zort/register-webhook")]
    public async Task<IActionResult> RegisterZortWebhook(
        [FromBody] RegisterZortWebhookRequest request,
        [FromServices] RegisterZortWebhookHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new RegisterZortWebhookCommand(request.BaseUrl, request.Key1, request.Key2, request.Key3),
            cancellationToken);
        return NoContent();
    }
}
