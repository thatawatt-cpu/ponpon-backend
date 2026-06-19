namespace PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;

public sealed record FlashSaleResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string[] Slots,
    string Status,
    IReadOnlyCollection<FlashSaleProductResponse> Products);

public sealed record FlashSaleProductResponse(
    Guid ProductId,
    decimal SalePrice,
    string ProductName,
    decimal? OriginalPrice,
    string? ImageUrl);
