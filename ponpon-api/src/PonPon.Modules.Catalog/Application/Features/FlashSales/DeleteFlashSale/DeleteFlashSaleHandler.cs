using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.FlashSales.DeleteFlashSale;

public sealed class DeleteFlashSaleHandler
{
    private readonly IFlashSaleRepository _flashSales;
    private readonly ICatalogUnitOfWork _unitOfWork;

    public DeleteFlashSaleHandler(IFlashSaleRepository flashSales, ICatalogUnitOfWork unitOfWork)
    {
        _flashSales = flashSales;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeleteFlashSaleCommand command, CancellationToken cancellationToken = default)
    {
        var flashSale = await _flashSales.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Flash sale was not found.");

        await _flashSales.DeleteProductsAsync(flashSale.Id, cancellationToken);
        await _flashSales.DeleteAsync(flashSale.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
