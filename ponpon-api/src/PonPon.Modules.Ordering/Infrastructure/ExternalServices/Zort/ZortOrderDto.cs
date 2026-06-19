using System.Text.Json;
using System.Text.Json.Serialization;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed class ZortOrderDto
{
    [JsonPropertyName("id")] public JsonElement Id { get; set; }
    [JsonPropertyName("number")] public string? Number { get; set; }
    [JsonPropertyName("customerid")] public JsonElement CustomerId { get; set; }
    [JsonPropertyName("customercode")] public string? CustomerCode { get; set; }
    [JsonPropertyName("customername")] public string? CustomerName { get; set; }
    [JsonPropertyName("customeridnumber")] public string? CustomerIdNumber { get; set; }
    [JsonPropertyName("customeremail")] public string? CustomerEmail { get; set; }
    [JsonPropertyName("customerphone")] public string? CustomerPhone { get; set; }
    [JsonPropertyName("customeraddress")] public string? CustomerAddress { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("paymentstatus")] public string? PaymentStatus { get; set; }
    [JsonPropertyName("amount")] public JsonElement Amount { get; set; }
    [JsonPropertyName("vatamount")] public JsonElement VatAmount { get; set; }
    [JsonPropertyName("shippingamount")] public JsonElement ShippingAmount { get; set; }
    [JsonPropertyName("paymentamount")] public JsonElement PaymentAmount { get; set; }
    [JsonPropertyName("discountamount")] public JsonElement DiscountAmount { get; set; }
    [JsonPropertyName("shippingchannel")] public string? ShippingChannel { get; set; }
    [JsonPropertyName("shippingname")] public string? ShippingName { get; set; }
    [JsonPropertyName("shippingaddress")] public string? ShippingAddress { get; set; }
    [JsonPropertyName("shippingphone")] public string? ShippingPhone { get; set; }
    [JsonPropertyName("trackingno")] public string? TrackingNo { get; set; }
    [JsonPropertyName("orderdate")] public JsonElement OrderDate { get; set; }
    [JsonPropertyName("orderdateString")] public string? OrderDateString { get; set; }
    [JsonPropertyName("shippingdate")] public JsonElement ShippingDate { get; set; }
    [JsonPropertyName("shippingdateString")] public string? ShippingDateString { get; set; }
    [JsonPropertyName("reference")] public string? Reference { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("saleschannel")] public string? SalesChannel { get; set; }
    [JsonPropertyName("integrationCustomerId")] public string? IntegrationCustomerId { get; set; }
    [JsonPropertyName("integrationCustomer")] public string? IntegrationCustomer { get; set; }
    [JsonPropertyName("warehousecode")] public string? WarehouseCode { get; set; }
    [JsonPropertyName("isCOD")] public JsonElement IsCod { get; set; }
    [JsonPropertyName("currency")] public string? Currency { get; set; }
    [JsonPropertyName("tag")] public JsonElement Tags { get; set; }
    [JsonPropertyName("createdatetime")] public JsonElement CreateDateTime { get; set; }
    [JsonPropertyName("createdatetimeString")] public string? CreateDateTimeString { get; set; }
    [JsonPropertyName("updatedatetime")] public JsonElement UpdateDateTime { get; set; }
    [JsonPropertyName("updatedatetimeString")] public string? UpdateDateTimeString { get; set; }
    [JsonPropertyName("list")] public IReadOnlyCollection<ZortOrderItemDto>? Items { get; set; }
    [JsonPropertyName("payments")] public IReadOnlyCollection<ZortOrderPaymentDto>? Payments { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
