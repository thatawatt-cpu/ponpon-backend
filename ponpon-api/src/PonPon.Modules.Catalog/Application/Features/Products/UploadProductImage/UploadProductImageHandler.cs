using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.Products.UploadProductImage;

public sealed class UploadProductImageHandler
{
    private readonly IProductRepository _products;
    private readonly ISupabaseStorageService _storage;

    public UploadProductImageHandler(IProductRepository products, ISupabaseStorageService storage)
    {
        _products = products;
        _storage = storage;
    }

    public async Task<UploadProductImageResponse> HandleAsync(UploadProductImageCommand command, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        var path = $"products/{product.Id}/{Guid.NewGuid()}{extension}";

        var url = await _storage.UploadAsync(path, command.FileStream, command.ContentType, cancellationToken);
        return new UploadProductImageResponse(url);
    }
}
