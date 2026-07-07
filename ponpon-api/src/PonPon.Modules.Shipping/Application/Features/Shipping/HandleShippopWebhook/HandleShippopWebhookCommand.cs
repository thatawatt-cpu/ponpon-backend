namespace PonPon.Modules.Shipping.Application.Features.Shipping.HandleShippopWebhook;

public sealed record HandleShippopWebhookCommand(
    string TrackingCode,
    string OrderStatus,
    string? CourierTrackingCode,
    string? StatusDateTime,
    IReadOnlyDictionary<string, string> RawFields);
