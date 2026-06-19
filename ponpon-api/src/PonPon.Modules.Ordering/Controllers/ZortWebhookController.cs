using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PonPon.Modules.Ordering.Application.Features.Orders.HandleZortWebhook;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Controllers;

[ApiController]
[Route("api/webhooks/zort")]
[AllowAnonymous]
public sealed class ZortWebhookController : ControllerBase
{
    private static readonly HashSet<string> OrderEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADDORDER", "UPDATEORDER", "DELETEORDER", "UPDATEORDERTRACKING", "UPDATEORDERPAYMENT"
    };

    [HttpPost]
    public async Task<IActionResult> HandleWebhook(
        [FromQuery] string method,
        [FromQuery] string? id,
        [FromServices] HandleZortWebhookHandler handler,
        [FromServices] IOptions<ZortOrderOptions> options,
        CancellationToken cancellationToken)
    {
        if (!VerifyWebhookKey(options.Value))
            return Unauthorized();

        if (!OrderEvents.Contains(method))
            return Ok();

        var zortOrderId = await ExtractZortOrderIdAsync(id, cancellationToken);
        if (zortOrderId is null)
            return BadRequest("Could not determine Zort order ID.");

        try
        {
            await handler.HandleAsync(new HandleZortWebhookCommand(zortOrderId.Value), cancellationToken);
        }
        catch (BadRequestException)
        {
            // Order no longer accessible in Zort (e.g. hard-deleted), acknowledge to stop retries
        }

        return Ok();
    }

    private bool VerifyWebhookKey(ZortOrderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookKey))
            return true;

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return false;

        return string.Equals(authHeader.ToString(), $"Basic {options.WebhookKey}", StringComparison.Ordinal);
    }

    private async Task<long?> ExtractZortOrderIdAsync(string? queryId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(queryId) && long.TryParse(queryId, out var fromQuery))
            return fromQuery;

        string payloadJson;
        var contentType = Request.ContentType ?? string.Empty;

        if (contentType.Contains("application/x-www-form-urlencoded"))
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            payloadJson = form.TryGetValue("payload", out var formPayload)
                ? formPayload.ToString()
                : string.Empty;
        }
        else
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            payloadJson = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var name in new[] { "id", "orderid" })
            {
                if (!root.TryGetProperty(name, out var prop))
                    continue;

                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var numberId))
                    return numberId;

                if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var parsedId))
                    return parsedId;
            }
        }
        catch (JsonException)
        {
            // Malformed payload
        }

        return null;
    }
}
