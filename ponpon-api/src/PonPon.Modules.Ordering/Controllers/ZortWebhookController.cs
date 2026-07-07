using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PonPon.Modules.Ordering.Application.Features.Orders.HandleZortWebhook;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Controllers;

[ApiController]
[Route("api/webhooks/zort/order")]
[AllowAnonymous]
public sealed class ZortWebhookController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> HandleWebhook(
        [FromQuery] string? method,
        [FromQuery] string? id,
        [FromQuery] string? orderid,
        [FromQuery] string? status,
        [FromQuery] string? paymentstatus,
        [FromServices] IOptions<ZortOrderOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortWebhookController> logger,
        CancellationToken cancellationToken)
    {
        var effectiveMethod = string.IsNullOrWhiteSpace(method) ? "UPDATEORDER" : method;

        if (!VerifyWebhookKey(options.Value))
        {
            logger.LogWarning("Zort webhook rejected: invalid key1. Method={Method}", effectiveMethod);
            return Unauthorized();
        }

        if (!string.Equals(effectiveMethod, "UPDATEORDER", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Zort webhook ignored: unrecognized method={Method}", effectiveMethod);
            return Ok();
        }

        var payload = await ReadPayloadAsync(id ?? orderid, status, paymentstatus, cancellationToken);
        if (payload.ZortOrderId is null)
        {
            logger.LogWarning("Zort webhook {Method}: could not determine order ID", effectiveMethod);
            return BadRequest("Could not determine Zort order ID.");
        }

        var hasStatus = !string.IsNullOrWhiteSpace(payload.Status);
        var hasPaymentStatus = !string.IsNullOrWhiteSpace(payload.PaymentStatus);

        if (hasStatus && hasPaymentStatus && (!IsPacked(payload.Status) || !IsPaid(payload.PaymentStatus)))
        {
            logger.LogDebug(
                "Zort webhook ignored: Method={Method} ZortOrderId={ZortOrderId} Status={Status} PaymentStatus={PaymentStatus}",
                effectiveMethod,
                payload.ZortOrderId,
                payload.Status,
                payload.PaymentStatus);
            return Ok();
        }

        logger.LogInformation("Zort webhook received: Method={Method} ZortOrderId={ZortOrderId}", effectiveMethod, payload.ZortOrderId);

        var command = new HandleZortWebhookCommand(payload.ZortOrderId.Value, effectiveMethod);
        queue.Enqueue(async (sp, ct) =>
        {
            var handler = sp.GetRequiredService<HandleZortWebhookHandler>();
            await handler.HandleAsync(command, ct);
        });

        return Ok();
    }

    private bool VerifyWebhookKey(ZortOrderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookKey))
            return true;

        if (!Request.Headers.TryGetValue("key1", out var key1))
            return false;

        return string.Equals(key1.ToString(), options.WebhookKey, StringComparison.Ordinal);
    }

    private async Task<ZortWebhookPayload> ReadPayloadAsync(
        string? queryId,
        string? queryStatus,
        string? queryPaymentStatus,
        CancellationToken cancellationToken)
    {
        long? zortOrderId = null;
        if (!string.IsNullOrEmpty(queryId) && long.TryParse(queryId, out var fromQuery))
            zortOrderId = fromQuery;

        string payloadJson;
        var contentType = Request.ContentType ?? string.Empty;

        if (contentType.Contains("application/x-www-form-urlencoded"))
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            if (form.TryGetValue("payload", out var formPayload) && !string.IsNullOrWhiteSpace(formPayload.ToString()))
            {
                payloadJson = formPayload.ToString();
            }
            else
            {
                if (zortOrderId is null)
                {
                    var formId = ReadFormValue(form, "id") ?? ReadFormValue(form, "orderid");
                    if (long.TryParse(formId, out var parsedId))
                        zortOrderId = parsedId;
                }

                return new ZortWebhookPayload(
                    zortOrderId,
                    ReadFormValue(form, "status") ?? queryStatus,
                    ReadFormValue(form, "paymentstatus") ?? queryPaymentStatus);
            }
        }
        else
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            payloadJson = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
            return new ZortWebhookPayload(zortOrderId, queryStatus, queryPaymentStatus);

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return new ZortWebhookPayload(zortOrderId, null, null);

            foreach (var name in new[] { "id", "orderid" })
            {
                if (!TryGetProperty(root, name, out var prop))
                    continue;

                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var numberId))
                    zortOrderId = numberId;

                if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var parsedId))
                    zortOrderId = parsedId;
            }

            return new ZortWebhookPayload(
                zortOrderId,
                ReadString(root, "status") ?? queryStatus,
                ReadString(root, "paymentstatus") ?? queryPaymentStatus);
        }
        catch (JsonException)
        {
            // Malformed payload
        }

        return new ZortWebhookPayload(zortOrderId, queryStatus, queryPaymentStatus);
    }

    private static bool IsPacked(string? status)
        => string.Equals(status, "Packed", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "5", StringComparison.OrdinalIgnoreCase);

    private static bool IsPaid(string? paymentStatus)
        => string.Equals(paymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
           || string.Equals(paymentStatus, "1", StringComparison.OrdinalIgnoreCase);

    private static string? ReadString(JsonElement root, string propertyName)
        => TryGetProperty(root, propertyName, out var property)
            ? property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            }
            : null;

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? ReadFormValue(IFormCollection form, string key)
    {
        foreach (var item in form)
        {
            if (item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                return item.Value.ToString();
        }

        return null;
    }

    private sealed record ZortWebhookPayload(long? ZortOrderId, string? Status, string? PaymentStatus);
}
