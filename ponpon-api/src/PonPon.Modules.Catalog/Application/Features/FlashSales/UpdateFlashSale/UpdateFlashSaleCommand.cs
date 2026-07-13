namespace PonPon.Modules.Catalog.Application.Features.FlashSales.UpdateFlashSale;

public sealed record UpdateFlashSaleCommand(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    string[] Slots,
    IReadOnlyList<(Guid ProductId, decimal SalePrice, int? QuantityLimit)> Products);
