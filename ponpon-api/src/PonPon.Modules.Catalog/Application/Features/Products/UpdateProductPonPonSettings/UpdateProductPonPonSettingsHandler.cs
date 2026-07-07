using System.Text.Json;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.Products.UpdateProductPonPonSettings;

public sealed class UpdateProductPonPonSettingsHandler
{
    private readonly IProductRepository _products;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly IRichTextImageProcessor _imageProcessor;

    public UpdateProductPonPonSettingsHandler(
        IProductRepository products,
        ICatalogUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        IRichTextImageProcessor imageProcessor)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _imageProcessor = imageProcessor;
    }

    public async Task HandleAsync(UpdateProductPonPonSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);

        if (product is not null)
        {
            var richDescription = await _imageProcessor.ProcessAsync(command.RichDescription, command.ProductId, cancellationToken);
            product.UpdatePonPonSettings(
                command.Slug,
                command.OriginalPrice,
                command.PromotionBadge,
                command.Highlights,
                richDescription,
                command.IsFeatured,
                command.IsBestSeller,
                command.IsOnHomepage,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var variant = await _products.GetVariantByIdAsync(command.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product or variant was not found.");

        var optionsJson = command.Options is { Count: > 0 }
            ? JsonSerializer.Serialize(command.Options)
            : null;

        variant.UpdatePonPonSettings(optionsJson, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
