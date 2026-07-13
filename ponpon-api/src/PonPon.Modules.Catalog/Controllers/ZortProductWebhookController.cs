using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [FromQuery] string? method,
        [FromQuery] string? id,
        [FromServices] IOptions<ZortOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
        => await HandleAsync(method, id, null, ProductEvents, options, queue, logger, cancellationToken);

    [HttpPost("api/webhooks/zort/product/add")]
    public async Task<IActionResult> HandleAddProductWebhook(
        [FromQuery] string? method,
        [FromQuery] string? id,
        [FromServices] IOptions<ZortOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
        => await HandleAsync(method, id, "ADDPRODUCT", ProductEvents, options, queue, logger, cancellationToken);

    [HttpPost("api/webhooks/zort/product/update")]
    public async Task<IActionResult> HandleUpdateProductWebhook(
        [FromQuery] string? method,
        [FromQuery] string? id,
        [FromServices] IOptions<ZortOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
        => await HandleAsync(method, id, "UPDATEPRODUCT", ProductEvents, options, queue, logger, cancellationToken);

    [HttpPost("api/webhooks/zort/product/delete")]
    public async Task<IActionResult> HandleDeleteProductWebhook(
        [FromQuery] string? method,
        [FromQuery] string? id,
        [FromServices] IOptions<ZortOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
        => await HandleAsync(method, id, "DELETEPRODUCT", ProductEvents, options, queue, logger, cancellationToken);

    [HttpPost("api/webhooks/zort/product-quantity")]
    public async Task<IActionResult> HandleProductQuantityWebhook(
        [FromQuery] string? method,
        [FromQuery] string? id,
        [FromServices] IOptions<ZortOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
        => await HandleAsync(method, id, null, QuantityEvents, options, queue, logger, cancellationToken);

    [HttpPost("api/webhooks/zort/product/quantity")]
    public async Task<IActionResult> HandleQuantityProductWebhook(
        [FromQuery] string? method,
        [FromQuery] string? id,
        [FromServices] IOptions<ZortOptions> options,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
        => await HandleAsync(method, id, "UPDATEQUANTITY", QuantityEvents, options, queue, logger, cancellationToken);

    private async Task<IActionResult> HandleAsync(
        string? queryMethod,
        string? queryId,
        string? fallbackMethod,
        IReadOnlySet<string> allowedMethods,
        IOptions<ZortOptions> options,
        IBackgroundTaskQueue queue,
        ILogger<ZortProductWebhookController> logger,
        CancellationToken cancellationToken)
    {
        var effectiveMethod = string.IsNullOrWhiteSpace(queryMethod) ? fallbackMethod : queryMethod;

        if (!VerifyWebhookKey(options.Value))
        {
            logger.LogWarning("Zort product webhook rejected: invalid key1. Method={Method}", effectiveMethod);
            return Unauthorized();
        }

        var payload = await ReadPayloadAsync(queryId, effectiveMethod, cancellationToken);
        effectiveMethod = string.IsNullOrWhiteSpace(payload.Method) ? fallbackMethod : payload.Method;

        if (string.IsNullOrWhiteSpace(effectiveMethod) || !allowedMethods.Contains(effectiveMethod))
        {
            logger.LogDebug("Zort product webhook ignored: unrecognized method={Method}", effectiveMethod);
            return Ok();
        }

        if (payload.ZortProductId is null)
        {
            logger.LogWarning("Zort product webhook {Method}: could not determine product ID", effectiveMethod);
            return BadRequest("Could not determine Zort product ID.");
        }

        logger.LogInformation("Zort product webhook received: Method={Method} ZortProductId={ZortProductId}", effectiveMethod, payload.ZortProductId);

        var command = new HandleZortProductWebhookCommand(payload.ZortProductId.Value, effectiveMethod);
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

    private async Task<ZortProductWebhookPayload> ReadPayloadAsync(
        string? queryId,
        string? queryMethod,
        CancellationToken cancellationToken)
    {
        long? zortProductId = null;
        if (TryParseId(queryId, out var fromQuery))
            zortProductId = fromQuery;

        var method = queryMethod;
        var contentType = Request.ContentType ?? string.Empty;

        if (contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            if (form.TryGetValue("payload", out var formPayload) && !string.IsNullOrWhiteSpace(formPayload.ToString()))
                return ReadJsonPayload(formPayload.ToString(), zortProductId, method);

            return new ZortProductWebhookPayload(
                zortProductId ?? ReadLong(ReadFormValue(form, "id") ?? ReadFormValue(form, "productid")),
                ReadFormValue(form, "method") ?? method);
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var payloadJson = await reader.ReadToEndAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(payloadJson)
            ? new ZortProductWebhookPayload(zortProductId, method)
            : ReadJsonPayload(payloadJson, zortProductId, method);
    }

    private static ZortProductWebhookPayload ReadJsonPayload(string payloadJson, long? fallbackId, string? fallbackMethod)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new ZortProductWebhookPayload(fallbackId, fallbackMethod);

            return new ZortProductWebhookPayload(
                ReadLong(root, "id") ?? ReadLong(root, "productid") ?? fallbackId,
                ReadString(root, "method") ?? fallbackMethod);
        }
        catch (JsonException)
        {
            return new ZortProductWebhookPayload(fallbackId, fallbackMethod);
        }
    }

    private static long? ReadLong(string? value)
        => long.TryParse(value, out var parsed) ? parsed : null;

    private static long? ReadLong(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt64(out var value) => value,
            JsonValueKind.String when long.TryParse(property.GetString(), out var value) => value,
            _ => null
        };
    }

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
            return true;

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

    private sealed record ZortProductWebhookPayload(long? ZortProductId, string? Method);
}
