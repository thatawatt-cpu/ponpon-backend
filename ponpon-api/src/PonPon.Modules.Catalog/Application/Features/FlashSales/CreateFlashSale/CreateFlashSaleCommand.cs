namespace PonPon.Modules.Catalog.Application.Features.FlashSales.CreateFlashSale;

public sealed record CreateFlashSaleCommand(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string[] Slots,
    IReadOnlyList<(Guid ProductId, decimal SalePrice)> Products);
