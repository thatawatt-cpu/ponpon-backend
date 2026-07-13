namespace PonPon.Modules.Catalog.Application.Features.FlashSales.CreateFlashSale;

public sealed record CreateFlashSaleCommand(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    string[] Slots,
    IReadOnlyList<(Guid ProductId, decimal SalePrice, int? QuantityLimit)> Products);
