using System.Text.Json;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

namespace PonPon.Modules.Ordering.Tests;

public sealed class ZortOrderMapperTests
{
    public void MapsOrderItemsAndPayments()
    {
        const string json = """
        {
          "id": "123",
          "number": "SO-001",
          "status": "Success",
          "paymentstatus": "Paid",
          "amount": "250.50",
          "saleschannel": "LineLiff",
          "isCOD": 1,
          "list": [
            {
              "productid": 10,
              "sku": "SKU-10",
              "name": "Tea",
              "number": 2,
              "pricepernumber": 100,
              "totalprice": 200
            }
          ],
          "payments": [
            {
              "id": 20,
              "name": "Transfer",
              "amount": 250.50,
              "paymentdatetimeString": "2026-06-15 10:30"
            }
          ]
        }
        """;

        var dto = JsonSerializer.Deserialize<ZortOrderDto>(json)
            ?? throw new InvalidOperationException("Could not deserialize order.");
        var snapshot = ZortOrderMapper.ToSnapshot(dto);

        AssertEqual(123L, snapshot.ZortOrderId);
        AssertEqual("SO-001", snapshot.Number);
        AssertEqual(250.50m, snapshot.Amount);
        AssertEqual("LineLiff", snapshot.SalesChannel);
        AssertEqual(true, snapshot.IsCod);
        AssertEqual(1, snapshot.Items.Count);
        AssertEqual("SKU-10", snapshot.Items.Single().Sku);
        AssertEqual(1, snapshot.Payments.Count);
        AssertEqual("Transfer", snapshot.Payments.Single().Name);
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, but was {actual}.");
        }
    }
}
