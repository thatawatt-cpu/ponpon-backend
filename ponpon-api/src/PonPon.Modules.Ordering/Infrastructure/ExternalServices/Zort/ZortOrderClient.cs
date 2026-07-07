using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed class ZortOrderClient : IZortOrderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ZortOrderOptions _options;

    public ZortOrderClient(HttpClient httpClient, IOptions<ZortOrderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<long> AddOrderAsync(
        ZortAddOrderRequest addOrderRequest,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Post, "Order/AddOrder")
        {
            Content = JsonContent.Create(addOrderRequest, options: JsonOptions)
        };
        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        EnsureZortSuccess(response, root, "ZORT AddOrder");

        return FindLong(root, "id")
            ?? FindLong(root, "orderid")
            ?? throw new BadRequestException(ReadString(root, "resDesc") ?? "ZORT AddOrder did not return an order id.");
    }

    public async Task<ZortOrderDto> GetOrderDetailAsync(
        long zortOrderId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"Order/GetOrderDetail?id={zortOrderId}");
        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(rawJson);
        EnsureZortSuccess(response, document.RootElement, "ZORT GetOrderDetail");
        var element = FindOrder(document.RootElement);
        return JsonSerializer.Deserialize<ZortOrderDto>(element.GetRawText(), JsonOptions)
            ?? throw new BadRequestException("ZORT GetOrderDetail returned an empty order.");
    }

    public async Task<ZortGetOrdersResponse> GetOrdersAsync(
        int page,
        int limit,
        string salesChannel,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var path = $"Order/GetOrders?saleschannel={Uri.EscapeDataString(salesChannel)}&page={page}&limit={limit}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(rawJson);
        EnsureZortSuccess(response, document.RootElement, "ZORT GetOrders");

        return ParseResponse(rawJson);
    }

    public async Task UpdateOrderPaymentAsync(
        string orderNumber,
        decimal amount,
        string paymentMethod,
        DateTime? paidAt,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var path = "Order/UpdateOrderPayment"
            + $"?number={Uri.EscapeDataString(orderNumber)}"
            + $"&paymentamount={Uri.EscapeDataString(amount.ToString(System.Globalization.CultureInfo.InvariantCulture))}"
            + $"&paymentmethod={Uri.EscapeDataString(paymentMethod)}";

        if (paidAt is DateTime paymentDate)
        {
            path += $"&paymentdate={Uri.EscapeDataString(paymentDate.ToString("yyyy-MM-dd HH:mm"))}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(rawJson);
        EnsureZortSuccess(response, document.RootElement, "ZORT UpdateOrderPayment");
    }

    public async Task UpdateOrderStatusAsync(
        string orderNumber,
        int status,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var path = "Order/UpdateOrderStatus"
            + $"?number={Uri.EscapeDataString(orderNumber)}"
            + $"&status={status}";

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(rawJson);
        EnsureZortSuccess(response, document.RootElement, "ZORT UpdateOrderStatus");
    }

    public async Task<ZortGetWebhookResponse> GetWebhookAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Get, "Webhook/GetWebhook");
        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        EnsureZortSuccess(response, root, "ZORT GetWebhook");
        var dataEl = root.TryGetProperty("data", out var d) ? d : root;

        return JsonSerializer.Deserialize<ZortGetWebhookResponse>(dataEl.GetRawText(), JsonOptions)
            ?? new ZortGetWebhookResponse();
    }

    public async Task RegisterWebhookAsync(string updateOrderUrl, string key1, string? key2 = null, string? key3 = null, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var body = new ZortRegisterWebhookRequest(
            updateOrderUrl,
            key1, key2, key3);
        using var request = new HttpRequestMessage(HttpMethod.Post, "Webhook/UpdateWebhook")
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(rawJson);
        EnsureZortSuccess(response, document.RootElement, "ZORT RegisterWebhook");
    }

    public async Task VoidOrderAsync(long zortOrderId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"Order/VoidOrder?id={zortOrderId}");
        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var resDesc = ReadString(root, "resDesc");

        if (string.Equals(
                resDesc?.Trim().TrimEnd('.'),
                "This order is voided",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        EnsureZortSuccess(response, root, "ZORT VoidOrder");
    }

    private static ZortGetOrdersResponse ParseResponse(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var list = FindList(root);
        var orders = list.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<IReadOnlyCollection<ZortOrderDto>>(list.GetRawText(), JsonOptions) ?? []
            : [];

        return new ZortGetOrdersResponse(
            orders,
            ReadInt(root, "count"),
            ReadDecimal(root, "totalAmount"),
            ReadDecimal(root, "totalPaymentAmount"),
            ReadString(root, "resCode"),
            ReadString(root, "resDesc"));
    }

    private static JsonElement FindList(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return default;
        }

        foreach (var name in new[] { "list", "orders", "data" })
        {
            if (!root.TryGetProperty(name, out var element))
            {
                continue;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                return element;
            }

            var nested = FindList(element);
            if (nested.ValueKind == JsonValueKind.Array)
            {
                return nested;
            }
        }

        return default;
    }

    private static JsonElement FindOrder(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new BadRequestException("ZORT GetOrderDetail returned invalid JSON.");
        }

        if (root.EnumerateObject().Any(x => x.Name.Equals("id", StringComparison.OrdinalIgnoreCase)))
        {
            return root;
        }

        foreach (var name in new[] { "data", "order", "detail" })
        {
            if (root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Object)
            {
                return element;
            }
        }

        throw new BadRequestException("ZORT GetOrderDetail returned an empty order.");
    }

    private static long? FindLong(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            var property = root.EnumerateObject()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            var element = property.Value;

            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number))
            {
                return number;
            }

            if (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    var nested = FindLong(property.Value, name);
                    if (nested.HasValue)
                    {
                        return nested;
                    }
                }
            }
        }

        return null;
    }

    private static int? ReadInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number)
            ? number
            : element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed)
                ? parsed
                : null;
    }

    private static decimal? ReadDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number)
            ? number
            : element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), out var parsed)
                ? parsed
                : null;
    }

    private static string? ReadString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var element) ? element.ToString() : null;
    }

    private static void EnsureZortSuccess(HttpResponseMessage response, JsonElement root, string operation)
    {
        var resDesc = ReadString(root, "resDesc");

        if (!response.IsSuccessStatusCode)
        {
            throw new BadRequestException(resDesc ?? $"{operation} failed ({(int)response.StatusCode}).");
        }

        var resCode = ReadString(root, "resCode");
        if (!string.IsNullOrWhiteSpace(resCode) && resCode != "0000" && resCode != "200")
        {
            throw new BadRequestException(resDesc ?? $"{operation} rejected (resCode: {resCode}).");
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.StoreName)
            || string.IsNullOrWhiteSpace(_options.ApiKey)
            || string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            throw new BadRequestException("ZORT credentials are not configured.");
        }
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("storename", _options.StoreName);
        request.Headers.TryAddWithoutValidation("apikey", _options.ApiKey);
        request.Headers.TryAddWithoutValidation("apisecret", _options.ApiSecret);
    }
}
