using System.Text.Json;
using System.Text.Json.Serialization;

namespace PonPon.Modules.Payment.Infrastructure.ExternalServices.Omise;

public sealed class OmiseChargeDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("paid")] public bool Paid { get; set; }
    [JsonPropertyName("paid_at")] public DateTime? PaidAt { get; set; }
    [JsonPropertyName("authorize_uri")] public string? AuthorizeUri { get; set; }
    [JsonPropertyName("failure_code")] public string? FailureCode { get; set; }
    [JsonPropertyName("failure_message")] public string? FailureMessage { get; set; }
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = string.Empty;
    [JsonPropertyName("expires_at")] public DateTime? ExpiresAt { get; set; }
    [JsonPropertyName("source")] public OmiseSourceDto? Source { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("refundable")] public bool Refundable { get; set; }
    [JsonPropertyName("refunded_amount")] public int RefundedAmount { get; set; }
    [JsonPropertyName("refunded")] public int LegacyRefundedAmount { get; set; }
    [JsonPropertyName("voided")] public bool Voided { get; set; }
}

public sealed class OmiseRefundDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("charge")] public string ChargeId { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = string.Empty;
    [JsonPropertyName("voided")] public bool Voided { get; set; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
}

public sealed class OmiseChargeSearchDto
{
    [JsonPropertyName("data")]
    public IReadOnlyCollection<OmiseChargeDto> Data { get; set; } = [];
}

public sealed class OmiseSourceDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("scannable_code")] public OmiseScannableCodeDto? ScannableCode { get; set; }
}

public sealed class OmiseScannableCodeDto
{
    [JsonPropertyName("image")] public OmiseImageDto? Image { get; set; }
}

public sealed class OmiseImageDto
{
    [JsonPropertyName("download_uri")] public string? DownloadUri { get; set; }
}

public sealed class OmiseErrorDto
{
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

public sealed class OmiseWebhookEvent
{
    [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;
    [JsonPropertyName("data")] public JsonElement Data { get; set; }
}
