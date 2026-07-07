using Microsoft.Extensions.Logging;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.Products.SyncSingleProductFromZort;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.Products.HandleZortProductWebhook;

public sealed class HandleZortProductWebhookHandler
{
    private readonly SyncSingleProductFromZortHandler _syncHandler;
    private readonly IProductRepository _products;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<HandleZortProductWebhookHandler> _logger;

    public HandleZortProductWebhookHandler(
        SyncSingleProductFromZortHandler syncHandler,
        IProductRepository products,
        ICatalogUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<HandleZortProductWebhookHandler> logger)
    {
        _syncHandler = syncHandler;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(HandleZortProductWebhookCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Method.Equals("DELETEPRODUCT", StringComparison.OrdinalIgnoreCase))
        {
            var product = await _products.GetByZortProductIdAsync(command.ZortProductId, cancellationToken);
            if (product is not null)
            {
                product.MarkMissingFromZort(_clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Zort {Method}: marked product ZortProductId={ZortProductId} as missing", command.Method, command.ZortProductId);
            }
            return;
        }

        await _syncHandler.HandleAsync(new SyncSingleProductFromZortCommand(command.ZortProductId), cancellationToken);
        _logger.LogInformation("Zort {Method}: synced product ZortProductId={ZortProductId}", command.Method, command.ZortProductId);
    }
}
