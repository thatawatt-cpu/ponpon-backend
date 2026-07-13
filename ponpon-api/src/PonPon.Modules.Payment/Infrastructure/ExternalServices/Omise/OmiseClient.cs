using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Infrastructure.ExternalServices.Omise;

public sealed class OmiseClient : IOmiseClient
{
    private readonly HttpClient _httpClient;
    private readonly OmiseOptions _options;
    private readonly IRuntimeSettingProvider _settings;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OmiseClient(
        HttpClient httpClient,
        IOptions<OmiseOptions> options,
        IRuntimeSettingProvider settings)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _settings = settings;
    }

    public Task<OmiseChargeResult> CreatePromptPayChargeAsync(int amount, string currency, string? description, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["amount"] = amount.ToString(),
            ["currency"] = currency,
            ["source[type]"] = "promptpay",
            ["expires_at"] = DateTimeOffset.UtcNow.AddMinutes(10).ToString("o"),
        };
        if (!string.IsNullOrWhiteSpace(description))
            form["description"] = description;

        return PostChargeAsync(form, cancellationToken);
    }

    public Task<OmiseChargeResult> CreateMobileBankingChargeAsync(string bankType, int amount, string currency, string? description, string returnUri, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["amount"] = amount.ToString(),
            ["currency"] = currency,
            ["source[type]"] = bankType,
            ["return_uri"] = returnUri,
        };
        if (!string.IsNullOrWhiteSpace(description))
            form["description"] = description;

        return PostChargeAsync(form, cancellationToken);
    }

    public Task<OmiseChargeResult> CreateCreditCardChargeAsync(string tokenId, int amount, string currency, string? description, string? returnUri, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["amount"] = amount.ToString(),
            ["currency"] = currency,
            ["card"] = tokenId,
            ["capture"] = "true",
        };
        if (!string.IsNullOrWhiteSpace(description))
            form["description"] = description;
        if (!string.IsNullOrWhiteSpace(returnUri))
            form["return_uri"] = returnUri;

        return PostChargeAsync(form, cancellationToken);
    }

    public async Task<OmiseChargeResult> GetChargeAsync(string chargeId, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"charges/{chargeId}", cancellationToken);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        var charge = await ReadChargeAsync(response, cancellationToken);
        return MapResult(charge);
    }

    public async Task<IReadOnlyCollection<OmiseChargeResult>> FindSuccessfulChargesByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken)
    {
        var charges = await FindChargesByOrderNumberAsync(orderNumber, cancellationToken);
        return charges
            .Where(x => x.Paid
                && string.Equals(x.Status, "successful", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<OmiseChargeResult>> FindChargesByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken)
    {
        var path = "search?scope=charge"
            + $"&query={Uri.EscapeDataString(orderNumber)}"
            + "&order=reverse_chronological"
            + "&per_page=100";
        using var request = await CreateRequestAsync(HttpMethod.Get, path, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonSerializer.Deserialize<OmiseErrorDto>(json, JsonOptions);
            throw new BadRequestException(error?.Message ?? "Omise charge search failed.");
        }

        var result = JsonSerializer.Deserialize<OmiseChargeSearchDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Omise charge search response.");

        return result.Data
            .Where(x => string.Equals(
                x.Description?.Trim(),
                orderNumber.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .Select(MapResult)
            .ToArray();
    }

    public async Task<OmiseRefundResult> CreateRefundAsync(
        string chargeId,
        int amount,
        string orderNumber,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            throw new BadRequestException("Refund amount must be greater than zero.");

        var form = new Dictionary<string, string>
        {
            ["amount"] = amount.ToString(),
            ["metadata[order_number]"] = orderNumber
        };

        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"charges/{Uri.EscapeDataString(chargeId)}/refunds",
            cancellationToken);
        request.Content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonSerializer.Deserialize<OmiseErrorDto>(json, JsonOptions);
            var message = error?.Message;
            if (string.Equals(message, "charge can't be refunded", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(
                    "Payment could not be refunded automatically by Omise. Please refund this order manually before cancelling it.");
            }

            throw new BadRequestException(message ?? "Omise refund request failed.");
        }

        var refund = JsonSerializer.Deserialize<OmiseRefundDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Omise refund response.");

        return new OmiseRefundResult(
            refund.Id,
            refund.ChargeId,
            refund.Status,
            refund.Amount,
            refund.Currency,
            refund.Voided,
            refund.CreatedAt);
    }

    private async Task<OmiseChargeResult> PostChargeAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "charges", cancellationToken);
        request.Content = new FormUrlEncodedContent(form);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        var charge = await ReadChargeAsync(response, cancellationToken);
        return MapResult(charge);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        var secretKey = await _settings.GetValueAsync("Omise", "SecretKey", cancellationToken)
            ?? _options.SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new BadRequestException("Omise secret key is not configured.");

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return request;
    }

    private async Task<OmiseChargeDto> ReadChargeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonSerializer.Deserialize<OmiseErrorDto>(json, JsonOptions);
            throw new BadRequestException(error?.Message ?? "Omise request failed.");
        }

        return JsonSerializer.Deserialize<OmiseChargeDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Omise charge response.");
    }

    private static OmiseChargeResult MapResult(OmiseChargeDto dto) => new(
        dto.Id,
        dto.Status,
        dto.Paid,
        dto.PaidAt,
        dto.AuthorizeUri,
        dto.Source?.ScannableCode?.Image?.DownloadUri,
        dto.FailureCode,
        dto.FailureMessage,
        dto.ExpiresAt,
        dto.Amount,
        dto.Currency,
        dto.Refundable,
        dto.RefundedAmount > 0 ? dto.RefundedAmount : dto.LegacyRefundedAmount,
        dto.Voided,
        dto.Source?.Type,
        dto.Description);
}
