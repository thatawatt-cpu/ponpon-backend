using System.Text.Json;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public static class ZortProductTagMatcher
{
    public const string LiffTag = "Lineliff";

    public static bool HasLiffTag(ZortProductDto product) => HasTag(product.Tag, LiffTag);

    public static bool HasLiffTag(string? rawZortJson)
    {
        if (string.IsNullOrWhiteSpace(rawZortJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawZortJson);
            return document.RootElement.TryGetProperty("tag", out var tagElement)
                && HasTag(tagElement, LiffTag);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool HasTag(JsonElement tagElement, string expectedTag)
    {
        if (tagElement.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return false;
        }

        if (tagElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in tagElement.EnumerateArray())
            {
                if (Matches(item, expectedTag))
                {
                    return true;
                }
            }

            return false;
        }

        return Matches(tagElement, expectedTag);
    }

    private static bool Matches(JsonElement element, string expectedTag)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(tag => string.Equals(tag, expectedTag, StringComparison.OrdinalIgnoreCase));
    }
}
