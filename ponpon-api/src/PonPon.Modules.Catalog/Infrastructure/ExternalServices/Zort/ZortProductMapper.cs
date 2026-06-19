using System.Globalization;
using System.Text.Json;
using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public static class ZortProductMapper
{
    public static ProductSnapshot ToSnapshot(ZortProductDto dto)
    {
        var rawJson = JsonSerializer.Serialize(dto);
        return new ProductSnapshot(
            ReadLong(dto.Id),
            ReadInt(dto.ProductType),
            dto.Name ?? string.Empty,
            EmptyToNull(dto.Description),
            dto.Sku ?? string.Empty,
            EmptyToNull(dto.Barcode),
            ReadDecimal(dto.SellPrice) ?? 0,
            ReadInt(dto.SellVatStatus),
            ReadDecimal(dto.PurchasePrice),
            ReadInt(dto.PurchaseVatStatus),
            ReadInt(dto.Stock),
            ReadInt(dto.AvailableStock),
            EmptyToNull(dto.UnitText),
            EmptyToNull(dto.ImagePath),
            ReadDecimal(dto.Weight),
            ReadDecimal(dto.Height),
            ReadDecimal(dto.Length),
            ReadDecimal(dto.Width),
            ReadLong(dto.CategoryId),
            EmptyToNull(dto.Category),
            ReadLong(dto.SubCategoryId),
            EmptyToNull(dto.SubCategory),
            ReadLong(dto.VariationId),
            ReadBool(dto.Active, defaultValue: true),
            rawJson);
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int ReadInt(JsonElement element) => (int)(ReadLong(element) ?? 0);

    private static long? ReadLong(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool ReadBool(JsonElement element, bool defaultValue)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return defaultValue;
        }

        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            return number != 0;
        }

        if (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsedNumber))
        {
            return parsedNumber != 0;
        }

        return defaultValue;
    }
}
