using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Application.Services;
using PonPon.Modules.Shipping.Domain;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Shipping.Application.Features.Shipping.HandleShippopWebhook;

public sealed class HandleShippopWebhookHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IShippingShipmentRepository _shipments;
    private readonly IShippingUnitOfWork _unitOfWork;
    private readonly IOrderShippingStatusUpdater _orderStatusUpdater;
    private readonly ILogger<HandleShippopWebhookHandler> _logger;

    public HandleShippopWebhookHandler(
        IShippingShipmentRepository shipments,
        IShippingUnitOfWork unitOfWork,
        IOrderShippingStatusUpdater orderStatusUpdater,
        ILogger<HandleShippopWebhookHandler> logger)
    {
        _shipments = shipments;
        _unitOfWork = unitOfWork;
        _orderStatusUpdater = orderStatusUpdater;
        _logger = logger;
    }

    public async Task HandleAsync(HandleShippopWebhookCommand command, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var eventDateTime = ParseDateTime(command.StatusDateTime);
        var rawPayloadJson = JsonSerializer.Serialize(command.RawFields, JsonOptions);
        var webhookEventKey = CreateWebhookEventKey(command);

        var (shipment, created) = await _shipments.GetOrCreateByShippopTrackingCodeAsync(
            command.TrackingCode,
            command.OrderStatus,
            now,
            cancellationToken);

        if (created)
        {
            _logger.LogWarning(
                "SHIPPOP webhook tracking code is not available locally yet; creating placeholder shipment. TrackingCode={TrackingCode} OrderStatus={OrderStatus}",
                command.TrackingCode,
                command.OrderStatus);
        }

        if (shipment.HasWebhookEvent(webhookEventKey))
        {
            _logger.LogInformation(
                "Duplicate SHIPPOP webhook ignored: TrackingCode={TrackingCode} OrderStatus={OrderStatus} EventKey={EventKey}",
                command.TrackingCode,
                command.OrderStatus,
                webhookEventKey);
            return;
        }

        var statusApplied = shipment.ApplyWebhook(
            command.OrderStatus,
            command.CourierTrackingCode,
            rawPayloadJson,
            eventDateTime,
            now);

        var shipmentEvent = ShippingShipmentEvent.Create(
            shipment.Id,
            command.TrackingCode,
            command.OrderStatus,
            command.CourierTrackingCode,
            eventDateTime,
            rawPayloadJson,
            now,
            webhookEventKey);

        if (statusApplied
            && shipment.OrderId is Guid orderId
            && ShippopOrderStatusPolicy.TryGetOrderProgress(command.OrderStatus, out var progress))
        {
            await _orderStatusUpdater.ApplyAsync(
                orderId,
                progress,
                command.CourierTrackingCode ?? command.TrackingCode,
                eventDateTime,
                cancellationToken);
        }

        if (ShippopOrderStatusPolicy.IsProblemStatus(command.OrderStatus))
        {
            _logger.LogWarning(
                "SHIPPOP shipment requires attention: TrackingCode={TrackingCode} OrderId={OrderId} OrderStatus={OrderStatus} Payload={Payload}",
                command.TrackingCode,
                shipment.OrderId,
                command.OrderStatus,
                rawPayloadJson);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var eventInserted = await _shipments.TryAddWebhookEventAsync(shipmentEvent, cancellationToken);

        if (!eventInserted)
        {
            _logger.LogInformation(
                "Concurrent duplicate SHIPPOP webhook event ignored: TrackingCode={TrackingCode} OrderStatus={OrderStatus} EventKey={EventKey}",
                command.TrackingCode,
                command.OrderStatus,
                webhookEventKey);
        }

        _logger.LogInformation(
            "SHIPPOP webhook processed: TrackingCode={TrackingCode} CourierTrackingCode={CourierTrackingCode} OrderStatus={OrderStatus} StatusDateTime={StatusDateTime}",
            command.TrackingCode,
            command.CourierTrackingCode,
            command.OrderStatus,
            command.StatusDateTime);
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsed))
        {
            return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
        }

        return null;
    }

    private static string CreateWebhookEventKey(HandleShippopWebhookCommand command)
    {
        var canonicalPayload = string.Join(
            "\n",
            command.RawFields
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{x.Key.Trim().ToLowerInvariant()}={x.Value.Trim()}"));
        var bytes = Encoding.UTF8.GetBytes(canonicalPayload);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
