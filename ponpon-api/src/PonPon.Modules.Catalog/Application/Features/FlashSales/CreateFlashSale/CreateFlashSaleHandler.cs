using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.FlashSales;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.FlashSales.CreateFlashSale;

public sealed class CreateFlashSaleHandler
{
    private readonly IFlashSaleRepository _flashSales;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CreateFlashSaleHandler(IFlashSaleRepository flashSales, ICatalogUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _flashSales = flashSales;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Guid> HandleAsync(CreateFlashSaleCommand command, CancellationToken cancellationToken = default)
    {
        FlashSaleValidator.Validate(
            command.Name, command.StartDate, command.EndDate, command.Slots, command.Products);
        var flashSale = FlashSale.Create(
            command.Name,
            command.StartDate,
            command.EndDate,
            command.Slots,
            command.Products,
            _clock.UtcNow);

        await _flashSales.AddAsync(flashSale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return flashSale.Id;
    }
}
