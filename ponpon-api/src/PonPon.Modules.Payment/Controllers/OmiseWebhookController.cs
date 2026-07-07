using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PonPon.Modules.Payment.Application.Features.HandleOmiseWebhook;
using PonPon.Modules.Payment.Infrastructure.ExternalServices.Omise;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Payment.Controllers;

[ApiController]
[Route("api/webhooks/omise")]
[AllowAnonymous]
public sealed class OmiseWebhookController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    public async Task<IActionResult> HandleWebhook(
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<OmiseWebhookController> logger,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payloadJson = await reader.ReadToEndAsync(cancellationToken);

        OmiseWebhookEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<OmiseWebhookEvent>(payloadJson, JsonOptions);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (evt is null || !IsSupportedChargeEvent(evt.Key))
        {
            if (evt is not null)
                logger.LogDebug("Omise webhook ignored: unsupported event key {EventKey}", evt.Key);

            return Ok();
        }

        OmiseChargeDto? charge;
        try
        {
            charge = evt.Data.Deserialize<OmiseChargeDto>(JsonOptions);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (charge is null)
            return Ok();

        var command = new HandleOmiseWebhookCommand(
            evt.Key, charge.Id, charge.Status,
            charge.Paid, charge.PaidAt, charge.Amount,
            charge.Description, charge.Source?.Type);

        logger.LogInformation(
            "Omise webhook received: EventKey={EventKey} ChargeId={ChargeId} Status={Status} Paid={Paid}",
            evt.Key, charge.Id, charge.Status, charge.Paid);

        queue.Enqueue(async (sp, ct) =>
        {
            var handler = sp.GetRequiredService<HandleOmiseWebhookHandler>();
            await handler.HandleAsync(command, ct);
        });

        return Ok();
    }

    private static bool IsSupportedChargeEvent(string eventKey)
        => eventKey is "charge.complete" or "charge.create";
}
