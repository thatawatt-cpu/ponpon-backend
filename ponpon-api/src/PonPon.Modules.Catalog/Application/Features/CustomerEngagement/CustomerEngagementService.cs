using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.Products;
using PonPon.Modules.Catalog.Application.Features.Products.GetProducts;
using PonPon.Modules.Catalog.Domain.CustomerEngagement;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.CustomerEngagement;

public sealed class CustomerEngagementService : ICustomerProfileSummaryProvider
{
    private const int RecentlyViewedLimit = 50;

    private readonly CatalogDbContext _db;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IProductSalesReadService _sales;
    private readonly ProductDetailPriceResolver _priceResolver;

    public CustomerEngagementService(
        CatalogDbContext db,
        ICatalogUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IProductSalesReadService sales,
        ProductDetailPriceResolver priceResolver)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _sales = sales;
        _priceResolver = priceResolver;
    }

    public async Task<WishlistResponse> GetWishlistAsync(CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var productIds = await GetActiveWishlistProductIdsAsync(customerId, cancellationToken);
        var products = await GetProductSummariesAsync(productIds, cancellationToken);
        return new WishlistResponse(productIds, products);
    }

    public async Task<WishlistResponse> AddWishlistAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        await EnsureActiveProductAsync(productId, cancellationToken);

        var exists = await _db.CustomerWishlistItems.AnyAsync(
            x => x.CustomerId == customerId && x.ProductId == productId,
            cancellationToken);
        if (!exists)
        {
            await _db.CustomerWishlistItems.AddAsync(
                CustomerWishlistItem.Create(customerId, productId, _clock.UtcNow),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await GetWishlistAsync(cancellationToken);
    }

    public async Task<WishlistResponse> DeleteWishlistAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        await _db.CustomerWishlistItems
            .Where(x => x.CustomerId == customerId && x.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);

        return await GetWishlistAsync(cancellationToken);
    }

    public async Task<RecentlyViewedResponse> GetRecentlyViewedAsync(CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var rows = await GetActiveRecentlyViewedRowsAsync(customerId, cancellationToken);
        var products = await GetProductSummariesAsync(rows.Select(x => x.ProductId).ToArray(), cancellationToken);
        return new RecentlyViewedResponse(rows, products);
    }

    public async Task<RecentlyViewedResponse> AddRecentlyViewedAsync(
        AddRecentlyViewedRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        await EnsureActiveProductAsync(request.ProductId, cancellationToken);

        var viewedAtUtc = request.ViewedAtUtc ?? _clock.UtcNow;
        var existing = await _db.CustomerRecentlyViewedProducts.FirstOrDefaultAsync(
            x => x.CustomerId == customerId && x.ProductId == request.ProductId,
            cancellationToken);

        if (existing is null)
        {
            await _db.CustomerRecentlyViewedProducts.AddAsync(
                CustomerRecentlyViewedProduct.Create(customerId, request.ProductId, viewedAtUtc),
                cancellationToken);
        }
        else
        {
            existing.Touch(viewedAtUtc);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TrimRecentlyViewedAsync(customerId, cancellationToken);
        return await GetRecentlyViewedAsync(cancellationToken);
    }

    public async Task<CustomerProfileSummaryCounts> GetCountsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var wishlistCount = await GetActiveWishlistQuery(customerId).CountAsync(cancellationToken);
        var recentlyViewedCount = await GetActiveRecentlyViewedQuery(customerId).CountAsync(cancellationToken);
        return new CustomerProfileSummaryCounts(
            WishlistCount: wishlistCount,
            RecentlyViewedCount: recentlyViewedCount);
    }

    private async Task<IReadOnlyCollection<Guid>> GetActiveWishlistProductIdsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
        => await GetActiveWishlistQuery(customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.ProductId)
            .ToArrayAsync(cancellationToken);

    private async Task<IReadOnlyCollection<RecentlyViewedItemResponse>> GetActiveRecentlyViewedRowsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
        => await GetActiveRecentlyViewedQuery(customerId)
            .OrderByDescending(x => x.ViewedAtUtc)
            .Take(RecentlyViewedLimit)
            .Select(x => new RecentlyViewedItemResponse(x.ProductId, x.ViewedAtUtc))
            .ToArrayAsync(cancellationToken);

    private IQueryable<CustomerWishlistItem> GetActiveWishlistQuery(Guid customerId)
        => _db.CustomerWishlistItems.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Where(x => _db.Products.Any(product =>
                product.Id == x.ProductId
                && product.IsActiveFromZort
                && product.IsVisibleOnLiff
                && product.Status == ProductStatus.Active
                && product.AvailableStock > 0));

    private IQueryable<CustomerRecentlyViewedProduct> GetActiveRecentlyViewedQuery(Guid customerId)
        => _db.CustomerRecentlyViewedProducts.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Where(x => _db.Products.Any(product =>
                product.Id == x.ProductId
                && product.IsActiveFromZort
                && product.IsVisibleOnLiff
                && product.Status == ProductStatus.Active
                && product.AvailableStock > 0));

    private async Task<IReadOnlyCollection<ProductListItemResponse>> GetProductSummariesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return [];

        var order = productIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        var products = await _db.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .Where(x => productIds.Contains(x.Id)
                        && x.IsActiveFromZort
                        && x.IsVisibleOnLiff
                        && x.Status == ProductStatus.Active
                        && x.AvailableStock > 0)
            .ToArrayAsync(cancellationToken);

        var soldCounts = await _sales.GetSoldCountsAsync(products.Select(x => x.Id).ToArray(), cancellationToken);
        var prices = await _priceResolver.ResolveAsync(products, cancellationToken);

        return products
            .OrderBy(x => order.GetValueOrDefault(x.Id))
            .Select(x => ProductListItemResponseFactory.Create(x, soldCounts, prices))
            .ToArray();
    }

    private async Task EnsureActiveProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var exists = await _db.Products.AnyAsync(
            x => x.Id == productId
                 && x.IsActiveFromZort
                 && x.IsVisibleOnLiff
                 && x.Status == ProductStatus.Active
                 && x.AvailableStock > 0,
            cancellationToken);

        if (!exists)
            throw new NotFoundException("Product was not found or is not active.");
    }

    private async Task TrimRecentlyViewedAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var overflowIds = await _db.CustomerRecentlyViewedProducts
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.ViewedAtUtc)
            .Skip(RecentlyViewedLimit)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        if (overflowIds.Length == 0)
            return;

        await _db.CustomerRecentlyViewedProducts
            .Where(x => overflowIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Guid GetCustomerId()
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserType != "Customer"
            || _currentUser.CustomerId is not Guid customerId)
        {
            throw new UnauthorizedException("Customer authentication is required.");
        }

        return customerId;
    }
}

public sealed record WishlistResponse(
    IReadOnlyCollection<Guid> ProductIds,
    IReadOnlyCollection<ProductListItemResponse> Products);

public sealed record AddRecentlyViewedRequest(Guid ProductId, DateTime? ViewedAtUtc = null);

public sealed record RecentlyViewedResponse(
    IReadOnlyCollection<RecentlyViewedItemResponse> Items,
    IReadOnlyCollection<ProductListItemResponse> Products);

public sealed record RecentlyViewedItemResponse(Guid ProductId, DateTime ViewedAtUtc);
