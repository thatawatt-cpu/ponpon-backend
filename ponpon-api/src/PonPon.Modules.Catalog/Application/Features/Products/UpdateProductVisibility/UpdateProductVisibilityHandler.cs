using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.Products.UpdateProductVisibility;

public sealed class UpdateProductVisibilityHandler
{
    private readonly IProductRepository _products;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateProductVisibilityHandler(IProductRepository products, ICatalogUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(UpdateProductVisibilityCommand command, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken) ?? throw new NotFoundException("Product was not found.");
        product.SetVisibility(command.IsVisibleOnLiff, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
