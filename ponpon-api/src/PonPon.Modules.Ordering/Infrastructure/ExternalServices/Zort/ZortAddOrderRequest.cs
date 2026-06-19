using System.Text.Json.Serialization;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed record ZortAddOrderRequest(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("uniquenumber")] string UniqueNumber,
    [property: JsonPropertyName("customername")] string CustomerName,
    [property: JsonPropertyName("customeremail")] string? CustomerEmail,
    [property: JsonPropertyName("customerphone")] string CustomerPhone,
    [property: JsonPropertyName("customeraddress")] string CustomerAddress,
    [property: JsonPropertyName("shippingname")] string ShippingName,
    [property: JsonPropertyName("shippingphone")] string ShippingPhone,
    [property: JsonPropertyName("shippingaddress")] string ShippingAddress,
    [property: JsonPropertyName("shippingchannel")] string? ShippingChannel,
    [property: JsonPropertyName("shippingamount")] decimal ShippingAmount,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("saleschannel")] string SalesChannel,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("vatamount")] decimal VatAmount,
    [property: JsonPropertyName("discount")] decimal Discount,
    [property: JsonPropertyName("list")] IReadOnlyCollection<ZortAddOrderItemRequest> Items);

public sealed record ZortAddOrderItemRequest(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("number")] int Quantity,
    [property: JsonPropertyName("pricepernumber")] decimal PricePerUnit,
    [property: JsonPropertyName("discount")] decimal Discount,
    [property: JsonPropertyName("totalprice")] decimal TotalPrice);
