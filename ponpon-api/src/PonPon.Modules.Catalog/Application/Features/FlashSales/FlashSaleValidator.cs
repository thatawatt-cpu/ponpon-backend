using System.Globalization;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.FlashSales;

internal static class FlashSaleValidator
{
    public static void Validate(
        string name,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<string> slots,
        IReadOnlyCollection<(Guid ProductId, decimal SalePrice, int? QuantityLimit)> products)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BadRequestException("Flash sale name is required.");
        if (startDate > endDate)
            throw new BadRequestException("Flash sale StartDate must not be after EndDate.");
        if (products.Count == 0)
            throw new BadRequestException("Flash sale requires at least one product.");
        if (products.Any(x => x.ProductId == Guid.Empty || x.SalePrice <= 0))
            throw new BadRequestException("Flash sale products require a valid ProductId and SalePrice greater than zero.");
        if (products.Any(x => x.QuantityLimit is <= 0))
            throw new BadRequestException("Flash sale QuantityLimit must be greater than zero when specified.");
        if (products.Select(x => x.ProductId).Distinct().Count() != products.Count)
            throw new BadRequestException("Flash sale products cannot contain duplicates.");
        if (slots.Any(x => !IsValidSlot(x)))
            throw new BadRequestException("Flash sale slots must use HH:mm-HH:mm format.");
    }

    private static bool IsValidSlot(string slot)
    {
        var parts = slot.Split('-', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && TimeOnly.TryParse(parts[0], CultureInfo.InvariantCulture, out _)
            && TimeOnly.TryParse(parts[1], CultureInfo.InvariantCulture, out _);
    }
}
