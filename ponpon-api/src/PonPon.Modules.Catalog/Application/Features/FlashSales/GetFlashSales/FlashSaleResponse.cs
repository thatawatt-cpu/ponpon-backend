namespace PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;

public sealed record FlashSaleResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    string[] Slots,
    string Status,
    IReadOnlyCollection<FlashSaleProductResponse> Products);

public sealed record FlashSaleProductResponse(
    Guid ProductId,
    decimal SalePrice,
    int? QuantityLimit,
    int ReservedQuantity,
    string ProductName,
    decimal? OriginalPrice,
    string? ImageUrl);
