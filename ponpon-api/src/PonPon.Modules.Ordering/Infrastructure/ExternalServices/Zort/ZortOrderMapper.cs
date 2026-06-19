using System.Globalization;
using System.Text.Json;
using PonPon.Modules.Ordering.Domain.Orders;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public static class ZortOrderMapper
{
    public static OrderSnapshot ToSnapshot(ZortOrderDto dto)
    {
        var zortOrderId = ReadLong(dto.Id)
            ?? throw new InvalidOperationException("ZORT order id is missing.");

        var items = dto.Items?.Select(ToItemSnapshot).ToArray() ?? [];
        var payments = dto.Payments?.Select(ToPaymentSnapshot).ToArray() ?? [];

        return new OrderSnapshot(
            zortOrderId,
            EmptyToNull(dto.Number) ?? $"ZORT-{zortOrderId}",
            ReadLong(dto.CustomerId),
            EmptyToNull(dto.CustomerCode),
            EmptyToNull(dto.CustomerName),
            EmptyToNull(dto.CustomerIdNumber),
            EmptyToNull(dto.CustomerEmail),
            EmptyToNull(dto.CustomerPhone),
            EmptyToNull(dto.CustomerAddress),
            EmptyToNull(dto.Status) ?? "Unknown",
            EmptyToNull(dto.PaymentStatus) ?? "Unknown",
            ReadDecimal(dto.Amount) ?? 0,
            ReadDecimal(dto.VatAmount) ?? 0,
            ReadDecimal(dto.ShippingAmount) ?? 0,
            ReadDecimal(dto.PaymentAmount) ?? 0,
            ReadDecimal(dto.DiscountAmount) ?? 0,
            EmptyToNull(dto.ShippingChannel),
            EmptyToNull(dto.ShippingName),
            EmptyToNull(dto.ShippingAddress),
            EmptyToNull(dto.ShippingPhone),
            EmptyToNull(dto.TrackingNo),
            ReadDateTime(dto.OrderDate, dto.OrderDateString),
            ReadDateTime(dto.ShippingDate, dto.ShippingDateString),
            EmptyToNull(dto.Reference),
            EmptyToNull(dto.Description),
            EmptyToNull(dto.SalesChannel) ?? string.Empty,
            EmptyToNull(dto.IntegrationCustomerId),
            EmptyToNull(dto.IntegrationCustomer),
            EmptyToNull(dto.WarehouseCode),
            ReadBool(dto.IsCod),
            EmptyToNull(dto.Currency),
            SerializeElement(dto.Tags),
            ReadDateTime(dto.CreateDateTime, dto.CreateDateTimeString),
            ReadDateTime(dto.UpdateDateTime, dto.UpdateDateTimeString),
            items,
            payments,
            JsonSerializer.Serialize(dto));
    }

    private static OrderItemSnapshot ToItemSnapshot(ZortOrderItemDto dto)
    {
        return new OrderItemSnapshot(
            ReadLong(dto.ProductId),
            EmptyToNull(dto.Sku) ?? string.Empty,
            EmptyToNull(dto.Name) ?? "Untitled product",
            ReadDecimal(dto.Number) ?? 0,
            EmptyToNull(dto.UnitText),
            ReadDecimal(dto.PricePerNumber) ?? 0,
            EmptyToNull(dto.Discount),
            ReadDecimal(dto.DiscountAmount) ?? 0,
            ReadDecimal(dto.TotalPrice) ?? 0,
            (int)(ReadLong(dto.ProductType) ?? 0),
            ReadLong(dto.BundleId),
            EmptyToNull(dto.BundleCode),
            EmptyToNull(dto.BundleName),
            JsonSerializer.Serialize(dto));
    }

    private static OrderPaymentSnapshot ToPaymentSnapshot(ZortOrderPaymentDto dto)
    {
        return new OrderPaymentSnapshot(
            ReadLong(dto.Id),
            EmptyToNull(dto.Name) ?? "Unknown",
            ReadDecimal(dto.Amount) ?? 0,
            ReadDateTime(dto.PaymentDateTime, dto.PaymentDateTimeString),
            JsonSerializer.Serialize(dto));
    }

    private static string? SerializeElement(JsonElement element)
    {
        return element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : element.GetRawText();
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static long? ReadLong(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number))
        {
            return number;
        }

        return element.ValueKind == JsonValueKind.String
            && long.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
    }

    private static decimal? ReadDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            return number;
        }

        return element.ValueKind == JsonValueKind.String
            && decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
    }

    private static bool ReadBool(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var number) => number != 0,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var value) => value,
            JsonValueKind.String when int.TryParse(element.GetString(), out var number) => number != 0,
            _ => false
        };
    }

    private static DateTime? ReadDateTime(JsonElement element, string? fallback)
    {
        if (element.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedElement))
        {
            return parsedElement.UtcDateTime;
        }

        if (!string.IsNullOrWhiteSpace(fallback)
            && DateTimeOffset.TryParse(fallback, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedFallback))
        {
            return parsedFallback.UtcDateTime;
        }

        return null;
    }
}
