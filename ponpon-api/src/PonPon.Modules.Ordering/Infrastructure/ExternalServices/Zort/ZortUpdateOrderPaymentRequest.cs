using System.Text.Json.Serialization;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed record ZortUpdateOrderPaymentRequest(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("paymentamount")] double Paymentamount,
    [property: JsonPropertyName("paymentmethod")] string Paymentmethod,
    [property: JsonPropertyName("paymentdate")] string? Paymentdate);
