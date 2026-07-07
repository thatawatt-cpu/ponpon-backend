using System.Text.Json.Serialization;

namespace PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;

public sealed record ShippopAddress(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("district")] string District,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("province")] string Province,
    [property: JsonPropertyName("postcode")] string Postcode,
    [property: JsonPropertyName("tel")] string Tel,
    [property: JsonPropertyName("email")] string Email);

public sealed record ShippopParcel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("weight")] double Weight,
    [property: JsonPropertyName("width")] double Width,
    [property: JsonPropertyName("length")] double Length,
    [property: JsonPropertyName("height")] double Height);

public sealed record ShippopPricelistRequest(
    [property: JsonPropertyName("api_key")] string ApiKey,
    [property: JsonPropertyName("data")] IReadOnlyDictionary<string, ShippopPricelistItem> Data);

public sealed record ShippopPricelistItem(
    [property: JsonPropertyName("from")] ShippopAddress From,
    [property: JsonPropertyName("to")] ShippopAddress To,
    [property: JsonPropertyName("parcel")] ShippopParcel Parcel,
    [property: JsonPropertyName("showall")] int ShowAll = 1,
    [property: JsonPropertyName("courier_code")] string? CourierCode = null,
    [property: JsonPropertyName("cod_amount")] decimal? CodAmount = null);

public sealed record ShippopBookingRequest(
    [property: JsonPropertyName("api_key")] string ApiKey,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("data")] IReadOnlyList<ShippopBookingItem> Data,
    [property: JsonPropertyName("force_confirm")] int ForceConfirm = 1);

public sealed record ShippopBookingItem(
    [property: JsonPropertyName("from")] ShippopAddress From,
    [property: JsonPropertyName("to")] ShippopAddress To,
    [property: JsonPropertyName("parcel")] ShippopParcel Parcel,
    [property: JsonPropertyName("courier_code")] string CourierCode,
    [property: JsonPropertyName("remark")] string? Remark,
    [property: JsonPropertyName("cod_amount")] decimal? CodAmount);

public sealed record ShippopCancelRequest(
    [property: JsonPropertyName("api_key")] string ApiKey,
    [property: JsonPropertyName("tracking_code")] string TrackingCode);

public sealed class ShippopRateDto
{
    public string CourierCode { get; set; } = string.Empty;
    public string CourierName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? EstimateTime { get; set; }
    public string? Remark { get; set; }
}

public sealed class ShippopBookingDto
{
    public int? PurchaseId { get; set; }
    public string TrackingCode { get; set; } = string.Empty;
    public string? CourierTrackingCode { get; set; }
    public string CourierCode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? LabelUrl { get; set; }
    public string? RawJson { get; set; }
}

public sealed class ShippopBookingDetailDto
{
    public string TrackingCode { get; set; } = string.Empty;
    public string? CourierTrackingCode { get; set; }
    public string CourierCode { get; set; } = string.Empty;
    public string? CourierName { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? LabelUrl { get; set; }
}
