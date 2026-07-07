using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Domain;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;

public sealed class ShippopClient : IShippopClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ShippopOptions _options;
    private readonly IShippopSenderRepository _senders;
    private readonly ILogger<ShippopClient> _logger;

    public ShippopClient(
        HttpClient httpClient,
        IOptions<ShippopOptions> options,
        IShippopSenderRepository senders,
        ILogger<ShippopClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _senders = senders;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ShippopRateDto>> CheckRatesAsync(
        ShippopAddress to,
        ShippopParcel parcel,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var sender = await GetRequiredSenderAsync(cancellationToken);

        var body = new ShippopPricelistRequest(
            _options.ApiKey,
            new Dictionary<string, ShippopPricelistItem>
            {
                ["0"] = new(BuildSenderAddress(sender), to, ToDomesticParcel(parcel))
            });

        using var response = await _httpClient.PostAsJsonAsync("pricelist/", body, JsonOptions, cancellationToken);
        using var document = await ReadJsonAsync(response, "SHIPPOP Pricelist", cancellationToken);

        EnsureShippopSuccess(document.RootElement, "SHIPPOP Pricelist");

        return ParseRates(document.RootElement);
    }

    public async Task<ShippopBookingDto> CreateBookingAsync(
        ShippopAddress to,
        ShippopParcel parcel,
        string courierCode,
        string serviceCode,
        string? remark,
        decimal cod,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var sender = await GetRequiredSenderAsync(cancellationToken);

        var selectedCourierCode = string.IsNullOrWhiteSpace(courierCode) ? serviceCode : courierCode;
        if (string.IsNullOrWhiteSpace(selectedCourierCode))
            throw new BadRequestException("Shippop courier code is required.");

        var body = new ShippopBookingRequest(
            _options.ApiKey,
            sender.Email,
            [
                new ShippopBookingItem(
                    BuildSenderAddress(sender),
                    to,
                    ToDomesticParcel(parcel),
                    selectedCourierCode,
                    EmptyToNull(remark),
                    cod > 0 ? cod : null)
            ],
            ForceConfirm: 1);

        using var response = await _httpClient.PostAsJsonAsync("booking/", body, JsonOptions, cancellationToken);
        using var document = await ReadJsonAsync(response, "SHIPPOP Booking", cancellationToken);

        EnsureShippopSuccess(document.RootElement, "SHIPPOP Booking");

        var booking = ParseBooking(document.RootElement)
            ?? throw new BadRequestException("Shippop did not return booking data.");
        booking.RawJson = document.RootElement.GetRawText();

        if (string.IsNullOrWhiteSpace(booking.CourierTrackingCode) && booking.PurchaseId.HasValue)
        {
            try
            {
                var confirmed = await ConfirmPurchaseAsync(booking.PurchaseId.Value, booking.TrackingCode, cancellationToken);
                booking.CourierTrackingCode = confirmed.CourierTrackingCode ?? booking.CourierTrackingCode;
                booking.CourierCode = string.IsNullOrWhiteSpace(confirmed.CourierCode) ? booking.CourierCode : confirmed.CourierCode;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "SHIPPOP booking was created but confirm failed. TrackingCode={TrackingCode} PurchaseId={PurchaseId}",
                    booking.TrackingCode,
                    booking.PurchaseId);
            }
        }

        return booking;
    }

    public async Task<ShippopBookingDetailDto> GetBookingAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["tracking_code"] = trackingCode
        });

        using var response = await _httpClient.PostAsync("tracking/", content, cancellationToken);
        using var document = await ReadJsonAsync(response, "SHIPPOP Tracking", cancellationToken);

        EnsureShippopSuccess(document.RootElement, "SHIPPOP Tracking");

        var root = document.RootElement;

        return new ShippopBookingDetailDto
        {
            TrackingCode = GetString(root, "tracking_code") ?? trackingCode,
            CourierTrackingCode = GetString(root, "courier_tracking_code"),
            CourierCode = GetString(root, "courier_code") ?? string.Empty,
            BookingStatus = GetString(root, "order_status") ?? string.Empty,
            Price = GetDecimal(root, "price"),
            LabelUrl = null
        };
    }

    public async Task CancelBookingAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var body = new ShippopCancelRequest(_options.ApiKey, trackingCode);
        using var response = await _httpClient.PostAsJsonAsync("cancel/", body, JsonOptions, cancellationToken);
        using var document = await ReadJsonAsync(response, "SHIPPOP Cancel", cancellationToken);

        EnsureShippopSuccess(document.RootElement, "SHIPPOP Cancel");
    }

    private async Task<ShippopBookingDto> ConfirmPurchaseAsync(
        int purchaseId,
        string trackingCode,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["api_key"] = _options.ApiKey,
            ["purchase_id"] = purchaseId.ToString(CultureInfo.InvariantCulture)
        });

        using var response = await _httpClient.PostAsync("confirm/", content, cancellationToken);
        using var document = await ReadJsonAsync(response, "SHIPPOP Confirm", cancellationToken);

        EnsureShippopSuccess(document.RootElement, "SHIPPOP Confirm");

        return ParseConfirmedBooking(document.RootElement, trackingCode)
            ?? throw new BadRequestException($"Shippop confirm did not return tracking {trackingCode}.");
    }

    private static ShippopAddress BuildSenderAddress(ShippopSender sender) => new(
        sender.Name,
        sender.Address,
        sender.District,
        sender.State,
        sender.Province,
        sender.Postcode,
        sender.Phone,
        sender.Email);

    private static ShippopParcel ToDomesticParcel(ShippopParcel parcel) => parcel with
    {
        Weight = Math.Ceiling(parcel.Weight * 1000)
    };

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new BadRequestException($"{operation} failed ({(int)response.StatusCode}): {json}");

        return JsonDocument.Parse(json);
    }

    private static void EnsureShippopSuccess(JsonElement root, string operation)
    {
        if (root.TryGetProperty("status", out var status) &&
            status.ValueKind == JsonValueKind.False)
        {
            var message = GetString(root, "message") ?? GetString(root, "error") ?? root.GetRawText();
            throw new BadRequestException($"{operation} failed: {message}");
        }
    }

    private static IReadOnlyList<ShippopRateDto> ParseRates(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return [];

        var rates = new List<ShippopRateDto>();

        foreach (var shipmentProperty in data.EnumerateObject())
        {
            if (shipmentProperty.Value.ValueKind != JsonValueKind.Object)
                continue;

            if (TryParseRate(shipmentProperty.Value, out var directRate))
            {
                rates.Add(directRate);
                continue;
            }

            foreach (var courierProperty in shipmentProperty.Value.EnumerateObject())
            {
                if (TryParseRate(courierProperty.Value, out var rate))
                    rates.Add(rate);
            }
        }

        return rates
            .Where(x => !string.IsNullOrWhiteSpace(x.CourierCode) && x.Price >= 0)
            .ToArray();
    }

    private static bool TryParseRate(JsonElement element, out ShippopRateDto rate)
    {
        rate = new ShippopRateDto();

        if (element.ValueKind != JsonValueKind.Object)
            return false;

        if (element.TryGetProperty("available", out var available) &&
            available.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        var courierCode = GetString(element, "courier_code");
        if (string.IsNullOrWhiteSpace(courierCode))
            return false;

        rate = new ShippopRateDto
        {
            CourierCode = courierCode,
            CourierName = GetString(element, "courier_name") ?? courierCode,
            ServiceName = GetString(element, "courier_name") ?? courierCode,
            ServiceCode = courierCode,
            Price = GetDecimal(element, "price"),
            EstimateTime = GetString(element, "estimate_time"),
            Remark = GetString(element, "remark")
        };

        return true;
    }

    private static ShippopBookingDto? ParseBooking(JsonElement root)
    {
        var purchaseId = GetInt(root, "purchase_id");

        if (!root.TryGetProperty("data", out var data))
            return null;

        var booking = FirstObject<ShippopBookingDto>(data, TryParseBooking);
        if (booking is null)
            return null;

        booking.PurchaseId = purchaseId;
        return booking;
    }

    private static bool TryParseBooking(JsonElement element, out ShippopBookingDto booking)
    {
        booking = new ShippopBookingDto();

        if (element.ValueKind != JsonValueKind.Object)
            return false;

        if (element.TryGetProperty("status", out var status) &&
            status.ValueKind == JsonValueKind.False)
        {
            var message = GetString(element, "message") ?? element.GetRawText();
            throw new BadRequestException($"SHIPPOP Booking item failed: {message}");
        }

        var trackingCode = GetString(element, "tracking_code");
        if (string.IsNullOrWhiteSpace(trackingCode))
            return false;

        booking = new ShippopBookingDto
        {
            TrackingCode = trackingCode,
            CourierTrackingCode = GetString(element, "courier_tracking_code"),
            CourierCode = GetString(element, "courier_code") ?? string.Empty,
            Price = GetDecimal(element, "price"),
            LabelUrl = GetString(element, "label_url")
        };

        return true;
    }

    private static ShippopBookingDto? ParseConfirmedBooking(JsonElement root, string trackingCode)
    {
        if (!root.TryGetProperty("result", out var result))
            return null;

        return FirstObject(result, (JsonElement element, out ShippopBookingDto booking) =>
        {
            booking = new ShippopBookingDto();

            if (element.ValueKind != JsonValueKind.Object)
                return false;

            var itemTrackingCode = GetString(element, "tracking_code");
            if (!string.Equals(itemTrackingCode, trackingCode, StringComparison.OrdinalIgnoreCase))
                return false;

            if (element.TryGetProperty("status", out var status) &&
                status.ValueKind == JsonValueKind.False)
            {
                var message = GetString(element, "message") ?? element.GetRawText();
                throw new BadRequestException($"SHIPPOP Confirm item failed: {message}");
            }

            booking = new ShippopBookingDto
            {
                TrackingCode = itemTrackingCode ?? trackingCode,
                CourierTrackingCode = GetString(element, "courier_tracking_code"),
                CourierCode = GetString(element, "courier_code") ?? string.Empty
            };

            return true;
        });
    }

    private static T? FirstObject<T>(
        JsonElement element,
        TryParseElement<T> parser)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (parser(property.Value, out var value))
                    return value;
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (parser(item, out var value))
                    return value;
            }
        }

        return default;
    }

    private delegate bool TryParseElement<T>(JsonElement element, out T value);

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static decimal GetDecimal(JsonElement element, string propertyName)
    {
        var raw = GetString(element, propertyName);
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        var raw = GetString(element, propertyName);
        return int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new BadRequestException("Shippop API key is not configured.");
    }

    private async Task<ShippopSender> GetRequiredSenderAsync(CancellationToken cancellationToken)
    {
        return await _senders.GetAsync(cancellationToken)
            ?? throw new BadRequestException(
                "Shippop sender address is not configured. Configure it via PUT /api/admin/shipping/sender.");
    }
}
