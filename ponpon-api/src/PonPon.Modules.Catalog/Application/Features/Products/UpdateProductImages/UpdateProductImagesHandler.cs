using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.Products.UpdateProductImages;

public sealed class UpdateProductImagesHandler
{
    private readonly IProductRepository _products;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateProductImagesHandler(IProductRepository products, ICatalogUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(UpdateProductImagesCommand command, CancellationToken cancellationToken = default)
    {
        if (!await _products.ExistsAsync(command.ProductId, cancellationToken))
            throw new NotFoundException("Product was not found.");

        await _products.DeleteProductImagesAsync(command.ProductId, cancellationToken);

        if (command.Images.Count > 0)
        {
            var now = _clock.UtcNow;
            var images = command.Images
                .Select(x => new ProductImage(command.ProductId, x.Url, x.SortOrder, x.IsPrimary, now))
                .ToArray();
            await _products.AddProductImagesAsync(images, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
