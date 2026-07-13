using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed class ZortProductClient : IZortProductClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly ZortOptions _options;
    private readonly IRuntimeSettingProvider _settings;

    public ZortProductClient(
        HttpClient httpClient,
        IOptions<ZortOptions> options,
        IRuntimeSettingProvider settings)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _settings = settings;
    }

    public async Task<ZortGetWarehousesResponse> GetWarehousesAsync(int page = 1, int limit = 500, CancellationToken cancellationToken = default)
    {
        var options = await ResolveOptionsAsync(cancellationToken);
        EnsureConfigured(options);
        using var request = CreateRequest(HttpMethod.Get, $"Warehouse/GetWarehouses?page={page}&limit={limit}", options);
        AddZortHeaders(request, options);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureZortSuccess(response, rawJson, "ZORT GetWarehouses");

        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        var list = root.TryGetProperty("list", out var listEl) && listEl.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<IReadOnlyCollection<ZortWarehouseDto>>(listEl.GetRawText(), JsonOptions) ?? []
            : [];
        var count = root.TryGetProperty("count", out var countEl) && countEl.TryGetInt32(out var c) ? c : (int?)null;
        var resCode = root.TryGetProperty("resCode", out var rc) ? rc.GetString() : null;
        var resDesc = root.TryGetProperty("resDesc", out var rd) ? rd.GetString() : null;

        return new ZortGetWarehousesResponse(list, count, resCode, resDesc);
    }

    public async Task<ZortGetCategoriesResponse> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var options = await ResolveOptionsAsync(cancellationToken);
        EnsureConfigured(options);
        using var request = CreateRequest(HttpMethod.Get, "Product/GetCategorys", options);
        AddZortHeaders(request, options);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureZortSuccess(response, rawJson, "ZORT GetCategorys");

        return ParseGetCategories(rawJson);
    }

    public async Task<ZortGetProductsResponse> GetProductsAsync(int page, int limit, CancellationToken cancellationToken = default)
    {
        var options = await ResolveOptionsAsync(cancellationToken);
        EnsureConfigured(options);
        using var request = CreateRequest(HttpMethod.Get, $"Product/GetProducts?page={page}&limit={limit}", options);
        AddZortHeaders(request, options);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureZortSuccess(response, rawJson, "ZORT GetProducts");

        return ParseGetProducts(rawJson, page, limit);
    }

    public async Task<ZortProductDto> GetProductByIdAsync(long zortProductId, CancellationToken cancellationToken = default)
    {
        var options = await ResolveOptionsAsync(cancellationToken);
        EnsureConfigured(options);
        using var request = CreateRequest(HttpMethod.Get, $"Product/GetProductDetail?id={zortProductId}", options);
        AddZortHeaders(request, options);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureZortSuccess(response, rawJson, "ZORT GetProductDetail");

        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var productElement = FindProductElement(root);
        return JsonSerializer.Deserialize<ZortProductDto>(productElement.GetRawText(), JsonOptions)
            ?? throw new BadRequestException("ZORT GetProductDetail returned empty product.");
    }

    public Task<ZortApiResponse> UpdateProductStockListAsync(ZortStockUpdateRequest request, CancellationToken cancellationToken = default)
        => PostStockPlaceholderAsync("Product/UpdateProductStockList", request, cancellationToken);

    public Task<ZortApiResponse> IncreaseProductStockListAsync(ZortStockUpdateRequest request, CancellationToken cancellationToken = default)
        => PostStockPlaceholderAsync("Product/IncreaseProductStockList", request, cancellationToken);

    public Task<ZortApiResponse> DecreaseProductStockListAsync(ZortStockUpdateRequest request, CancellationToken cancellationToken = default)
        => PostStockPlaceholderAsync("Product/DecreaseProductStockList", request, cancellationToken);

    public Task<ZortApiResponse> UpdateProductAvailableStockListAsync(ZortStockUpdateRequest request, CancellationToken cancellationToken = default)
        => PostStockPlaceholderAsync("Product/UpdateProductAvailableStockList", request, cancellationToken);

    private async Task<ZortApiResponse> PostStockPlaceholderAsync(string path, ZortStockUpdateRequest request, CancellationToken cancellationToken)
    {
        var options = await ResolveOptionsAsync(cancellationToken);
        EnsureConfigured(options);
        using var httpRequest = CreateRequest(HttpMethod.Post, path, options);
        httpRequest.Content = JsonContent.Create(request, options: JsonOptions);
        AddZortHeaders(httpRequest, options);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var resCode = TryReadString(rawJson, "resCode");
        var resDesc = TryReadString(rawJson, "resDesc");
        var isSuccess = response.IsSuccessStatusCode && IsSuccessfulResCode(resCode);

        return new ZortApiResponse(isSuccess, resCode, resDesc, rawJson);
    }

    private void AddZortHeaders(HttpRequestMessage request, ResolvedZortOptions options)
    {
        request.Headers.TryAddWithoutValidation("storename", options.StoreName);
        request.Headers.TryAddWithoutValidation("apikey", options.ApiKey);
        request.Headers.TryAddWithoutValidation("apisecret", options.ApiSecret);
    }

    private static void EnsureConfigured(ResolvedZortOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl)
            || string.IsNullOrWhiteSpace(options.StoreName)
            || string.IsNullOrWhiteSpace(options.ApiKey)
            || string.IsNullOrWhiteSpace(options.ApiSecret))
        {
            throw new BadRequestException("ZORT credentials are not configured.");
        }
    }

    private async Task<ResolvedZortOptions> ResolveOptionsAsync(CancellationToken cancellationToken)
    {
        var settings = await _settings.GetGroupAsync("Zort", cancellationToken);
        return new ResolvedZortOptions(
            Get(settings, "BaseUrl", _options.BaseUrl),
            Get(settings, "StoreName", _options.StoreName),
            Get(settings, "ApiKey", _options.ApiKey),
            Get(settings, "ApiSecret", _options.ApiSecret));
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, ResolvedZortOptions options)
        => new(method, new Uri(new Uri(options.BaseUrl.TrimEnd('/') + "/"), path));

    private static string Get(IReadOnlyDictionary<string, string?> settings, string key, string fallback)
        => settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private sealed record ResolvedZortOptions(string BaseUrl, string StoreName, string ApiKey, string ApiSecret);

    private static ZortGetProductsResponse ParseGetProducts(string rawJson, int fallbackPage, int fallbackLimit)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var listElement = FindListElement(root);
        var products = listElement.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<IReadOnlyCollection<ZortProductDto>>(listElement.GetRawText(), JsonOptions) ?? []
            : [];

        return new ZortGetProductsResponse(
            products,
            TryReadInt(root, "count") ?? TryReadInt(root, "total"),
            TryReadInt(root, "page") ?? fallbackPage,
            TryReadInt(root, "limit") ?? fallbackLimit,
            TryReadString(root, "resCode"),
            TryReadString(root, "resDesc"));
    }

    private static ZortGetCategoriesResponse ParseGetCategories(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        var listElement = FindListElement(root);
        var categories = listElement.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<IReadOnlyCollection<ZortCategoryDto>>(listElement.GetRawText(), JsonOptions) ?? []
            : [];

        return new ZortGetCategoriesResponse(
            categories,
            TryReadString(root, "resCode"),
            TryReadString(root, "resDesc"));
    }

    private static JsonElement FindListElement(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        foreach (var name in new[] { "list", "products", "categorys", "categories", "data", "items" })
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var element))
            {
                if (element.ValueKind == JsonValueKind.Array)
                {
                    return element;
                }

                if (element.ValueKind == JsonValueKind.Object)
                {
                    var nested = FindListElement(element);
                    if (nested.ValueKind == JsonValueKind.Array)
                    {
                        return nested;
                    }
                }
            }
        }

        return default;
    }

    private static int? TryReadInt(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(name, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static JsonElement FindProductElement(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("ZORT GetProductDetail returned invalid JSON.");

        if (root.TryGetProperty("id", out _))
            return root;

        foreach (var name in new[] { "data", "product", "detail" })
        {
            if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Object)
                return el;
        }

        throw new BadRequestException("ZORT GetProductDetail returned empty product.");
    }

    private static string? TryReadString(string rawJson, string name)
    {
        try
        {
            using var document = JsonDocument.Parse(rawJson);
            return TryReadString(document.RootElement, name);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryReadString(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var element))
        {
            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
        }

        return null;
    }

    private static void EnsureZortSuccess(HttpResponseMessage response, string rawJson, string operation)
    {
        var resCode = TryReadString(rawJson, "resCode");
        var resDesc = TryReadString(rawJson, "resDesc");

        if (!response.IsSuccessStatusCode)
        {
            throw new BadRequestException(resDesc ?? $"{operation} failed ({(int)response.StatusCode}).");
        }

        if (!IsSuccessfulResCode(resCode))
        {
            throw new BadRequestException(resDesc ?? $"{operation} rejected (resCode: {resCode}).");
        }
    }

    private static bool IsSuccessfulResCode(string? resCode)
    {
        return string.IsNullOrWhiteSpace(resCode) || resCode == "0000" || resCode == "200";
    }
}
