using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PonPon.Modules.Catalog.Application.Features.Products.HandleZortProductWebhook;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class ZortProductWebhookController : ControllerBase
{
    private static readonly HashSet<string> ProductEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADDPRODUCT", "UPDATEPRODUCT", "DELETEPRODUCT"
    };

    private static readonly HashSet<string> QuantityEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "UPDATEQUANTITY"
    };

    [HttpPost("api/webhooks/zort/product")]
    public async Task<IActionResult> HandleProductWebhook(
        [FromQuery] string method,
        [FromQuery] string? id,
        [FromServices] IOptions<ZortOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
    {
        if (!VerifyWebhookKey(options.Value))
        {
            logger.LogWarning("Zort product webhook rejected: invalid key1. Method={Method}", method);
            return Unauthorized();
        }

        if (!ProductEvents.Contains(method))
        {
            logger.LogDebug("Zort product webhook ignored: unrecognized method={Method}", method);
            return Ok();
        }

        if (!TryParseId(id, out var zortProductId))
        {
            logger.LogWarning("Zort product webhook {Method}: could not determine product ID", method);
            return BadRequest("Could not determine Zort product ID.");
        }

        logger.LogInformation("Zort product webhook received: Method={Method} ZortProductId={ZortProductId}", method, zortProductId);

        var command = new HandleZortProductWebhookCommand(zortProductId, method);
        queue.Enqueue(async (sp, ct) =>
        {
            var handler = sp.GetRequiredService<HandleZortProductWebhookHandler>();
            await handler.HandleAsync(command, ct);
        });

        return Ok();
    }

    [HttpPost("api/webhooks/zort/product-quantity")]
    public async Task<IActionResult> HandleProductQuantityWebhook(
        [FromQuery] string method,
        [FromQuery] string? id,
        [FromServices] IOptions<ZortOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
    {
        if (!VerifyWebhookKey(options.Value))
        {
            logger.LogWarning("Zort quantity webhook rejected: invalid key1. Method={Method}", method);
            return Unauthorized();
        }

        if (!QuantityEvents.Contains(method))
        {
            logger.LogDebug("Zort quantity webhook ignored: unrecognized method={Method}", method);
            return Ok();
        }

        if (!TryParseId(id, out var zortProductId))
        {
            logger.LogWarning("Zort quantity webhook {Method}: could not determine product ID", method);
            return BadRequest("Could not determine Zort product ID.");
        }

        logger.LogInformation("Zort quantity webhook received: Method={Method} ZortProductId={ZortProductId}", method, zortProductId);

        var command = new HandleZortProductWebhookCommand(zortProductId, method);
        queue.Enqueue(async (sp, ct) =>
        {
            var handler = sp.GetRequiredService<HandleZortProductWebhookHandler>();
            await handler.HandleAsync(command, ct);
        });

        return Ok();
    }

    private bool VerifyWebhookKey(ZortOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookKey))
            return true;

        if (!Request.Headers.TryGetValue("key1", out var key1))
            return false;

        return string.Equals(key1.ToString(), options.WebhookKey, StringComparison.Ordinal);
    }

    private static bool TryParseId(string? id, out long result)
    {
        result = 0;
        return !string.IsNullOrEmpty(id) && long.TryParse(id, out result);
    }
}
