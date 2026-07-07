using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.Products.SyncSingleProductFromZort;

public sealed class SyncSingleProductFromZortHandler
{
    private readonly IZortProductClient _zortClient;
    private readonly IProductRepository _products;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public SyncSingleProductFromZortHandler(
        IZortProductClient zortClient,
        IProductRepository products,
        ICatalogUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _zortClient = zortClient;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(SyncSingleProductFromZortCommand command, CancellationToken cancellationToken = default)
    {
        var dto = await _zortClient.GetProductByIdAsync(command.ZortProductId, cancellationToken);

        if (!ZortProductTagMatcher.HasLiffTag(dto))
            return;

        ProductSnapshot snapshot;
        try
        {
            snapshot = ZortProductMapper.ToSnapshot(dto);
        }
        catch
        {
            return;
        }

        var parsedSku = Product.ParseSku(snapshot.Sku);
        var existing = await _products.GetByBaseSkusWithVariantsAsync(
            new HashSet<string>(StringComparer.Ordinal) { parsedSku.BaseSku },
            cancellationToken);

        var product = existing.FirstOrDefault()
            ?? await _products.GetByZortProductIdAsync(command.ZortProductId, cancellationToken);

        if (product is null)
        {
            product = Product.CreateFromZort(snapshot, _clock.UtcNow);
            product.UpsertVariant(snapshot, _clock.UtcNow);
            await _products.AddAsync(product, cancellationToken);
        }
        else
        {
            product.ApplyZortSnapshot(snapshot, _clock.UtcNow);
            product.UpsertVariant(snapshot, _clock.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
