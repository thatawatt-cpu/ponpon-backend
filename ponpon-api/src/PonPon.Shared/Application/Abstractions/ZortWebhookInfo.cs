namespace PonPon.Shared.Application.Abstractions;

public sealed record ZortWebhookInfo(
    string? AddOrderUrl,
    string? UpdateOrderUrl,
    string? DeleteOrderUrl,
    string? UpdateOrderTrackingUrl,
    string? UpdateOrderPaymentUrl,
    string? AddProductUrl,
    string? UpdateProductUrl,
    string? DeleteProductUrl,
    string? UpdateQuantityUrl,
    string? Key1,
    string? Key2,
    string? Key3);
