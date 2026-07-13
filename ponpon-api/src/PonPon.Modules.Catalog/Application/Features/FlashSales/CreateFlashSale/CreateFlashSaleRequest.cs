namespace PonPon.Modules.Catalog.Application.Features.FlashSales.CreateFlashSale;

public sealed record CreateFlashSaleRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    string[] Slots,
    IReadOnlyList<FlashSaleProductRequest> Products);

public sealed record FlashSaleProductRequest(Guid ProductId, decimal SalePrice, int? QuantityLimit);
