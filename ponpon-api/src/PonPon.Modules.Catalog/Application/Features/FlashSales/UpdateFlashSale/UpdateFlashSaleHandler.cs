using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.FlashSales.UpdateFlashSale;

public sealed class UpdateFlashSaleHandler
{
    private readonly IFlashSaleRepository _flashSales;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateFlashSaleHandler(IFlashSaleRepository flashSales, ICatalogUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _flashSales = flashSales;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(UpdateFlashSaleCommand command, CancellationToken cancellationToken = default)
    {
        FlashSaleValidator.Validate(
            command.Name, command.StartDate, command.EndDate, command.Slots, command.Products);
        var flashSale = await _flashSales.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Flash sale was not found.");

        await _flashSales.DeleteProductsAsync(flashSale.Id, cancellationToken);

        flashSale.Update(
            command.Name,
            command.StartDate,
            command.EndDate,
            command.Slots,
            command.Products,
            _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
