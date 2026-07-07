using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PonPon.Modules.Shipping.Application.Features.Shipping.HandleShippopWebhook;

namespace PonPon.Modules.Shipping.Controllers;

[ApiController]
[Route("api/webhooks/shippop")]
[AllowAnonymous]
public sealed class ShippopWebhookController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> HandleWebhook(
        [FromServices] HandleShippopWebhookHandler handler,
        [FromServices] ILogger<ShippopWebhookController> logger,
        [FromServices] IHostApplicationLifetime applicationLifetime,
        CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
            return BadRequest("SHIPPOP webhook must be form-urlencoded.");

        var form = await Request.ReadFormAsync(cancellationToken);
        var fields = form.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var trackingCode = GetValue(fields, "tracking_code");
        var orderStatus = GetValue(fields, "order_status");

        if (string.IsNullOrWhiteSpace(trackingCode) || string.IsNullOrWhiteSpace(orderStatus))
            return BadRequest("tracking_code and order_status are required.");

        var command = new HandleShippopWebhookCommand(
            trackingCode,
            orderStatus,
            GetValue(fields, "courier_tracking_code"),
            GetValue(fields, "data[datetime]"),
            fields);

        logger.LogInformation(
            "SHIPPOP webhook received: TrackingCode={TrackingCode} CourierTrackingCode={CourierTrackingCode} OrderStatus={OrderStatus}",
            command.TrackingCode,
            command.CourierTrackingCode,
            command.OrderStatus);

        // Once the payload has been read, persist it even if SHIPPOP closes the
        // HTTP connection while processing. Application shutdown can still cancel it.
        await handler.HandleAsync(command, applicationLifetime.ApplicationStopping);

        return Ok(new { success = 1 });
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> fields, string key)
        => fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
}
