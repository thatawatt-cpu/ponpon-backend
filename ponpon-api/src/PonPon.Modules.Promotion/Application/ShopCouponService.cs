using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PonPon.Modules.Promotion.Domain;
using PonPon.Modules.Promotion.Infrastructure;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Promotion.Application;

public sealed class ShopCouponService : IShopCouponService
{
    private const string DefaultSalesChannel = "line_liff";
    private readonly PromotionDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IMemoryCache _cache;

    public ShopCouponService(
        PromotionDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<ShopCouponResponse>> GetAvailableAsync(
        ShopCouponQuery query,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var now = _clock.UtcNow;
        var coupons = await GetBaseVisibleCoupons(now, query, cancellationToken);
        var couponIds = coupons.Select(x => x.Id).ToArray();
        var claims = await GetClaimsAsync(customerId, couponIds, cancellationToken);
        var usageCounts = await GetUsageCountsAsync(customerId, couponIds, cancellationToken);

        return coupons
            .Where(coupon => IsCustomerScopeVisible(coupon, customerId))
            .Select(coupon => ToResponse(
                coupon,
                claims.GetValueOrDefault(coupon.Id),
                CanClaim(coupon, claims.ContainsKey(coupon.Id), usageCounts.GetValueOrDefault(coupon.Id)),
                now))
            .OrderByDescending(x => x.CanClaim)
            .ThenBy(x => x.MinimumSubtotal)
            .ThenBy(x => x.Code)
            .ToArray();
    }

    public async Task<ShopCouponResponse> ClaimAsync(
        Guid couponId,
        ShopCouponQuery query,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var now = _clock.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var couponGlobalKey = BitConverter.ToInt64(couponId.ToByteArray(), 0);
        var customerKey = BitConverter.ToInt32(customerId.ToByteArray(), 0);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({couponGlobalKey})",
            cancellationToken);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({BitConverter.ToInt32(couponId.ToByteArray(), 0)}, {customerKey})",
            cancellationToken);

        var coupon = (await GetBaseVisibleCoupons(now, query, cancellationToken))
            .FirstOrDefault(x => x.Id == couponId)
            ?? throw new NotFoundException("Coupon was not found or is not available.");
        if (!IsCustomerScopeVisible(coupon, customerId))
            throw new BadRequestException("Coupon is not applicable to this customer.");

        var existingClaim = await _db.CouponClaims
            .FirstOrDefaultAsync(
                x => x.CouponId == couponId && x.CustomerId == customerId,
                cancellationToken);
        if (existingClaim is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(coupon, existingClaim, canClaim: false, now);
        }

        var usageCount = await _db.CouponUsages.CountAsync(
            x => x.CouponId == couponId
                 && x.CustomerId == customerId
                 && !x.IsReleased,
            cancellationToken);
        if (!CanClaim(coupon, isClaimed: false, usageCount))
            throw new BadRequestException("Coupon cannot be claimed.");

        var claim = CouponClaim.Create(couponId, customerId, now);
        await _db.CouponClaims.AddAsync(claim, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToResponse(coupon, claim, canClaim: false, now);
    }

    public async Task<IReadOnlyCollection<ShopCouponResponse>> GetMyCouponsAsync(
        ShopCouponQuery query,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var now = _clock.UtcNow;
        var claimedCouponIds = await _db.CouponClaims.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => x.CouponId)
            .ToArrayAsync(cancellationToken);
        if (claimedCouponIds.Length == 0)
            return [];

        var coupons = await GetBaseVisibleCoupons(now, query, cancellationToken);
        coupons = coupons.Where(x => claimedCouponIds.Contains(x.Id)).ToArray();
        var claims = await GetClaimsAsync(customerId, coupons.Select(x => x.Id).ToArray(), cancellationToken);
        var usageCounts = await GetUsageCountsAsync(customerId, coupons.Select(x => x.Id).ToArray(), cancellationToken);

        return coupons
            .Select(coupon => ToResponse(
                coupon,
                claims.GetValueOrDefault(coupon.Id),
                CanClaim(coupon, isClaimed: true, usageCounts.GetValueOrDefault(coupon.Id)),
                now))
            .OrderBy(x => x.EndsAtUtc ?? DateTime.MaxValue)
            .ThenBy(x => x.Code)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<Coupon>> GetBaseVisibleCoupons(
        DateTime now,
        ShopCouponQuery query,
        CancellationToken cancellationToken)
    {
        var salesChannel = Normalize(query.SalesChannel) ?? DefaultSalesChannel;
        var paymentMethod = Normalize(query.PaymentMethod);
        var shippingChannel = Normalize(query.ShippingChannel);
        var cacheKey = BuildBaseCouponCacheKey(now, query, salesChannel, paymentMethod, shippingChannel);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyCollection<Coupon>? cachedCoupons)
            && cachedCoupons is not null)
        {
            return cachedCoupons;
        }

        var coupons = await _db.Coupons.AsNoTracking()
            .Include(x => x.Scopes)
            .Include(x => x.CustomerScopes)
            .Include(x => x.Conditions)
            .Where(x => x.IsActive
                        && (!x.StartsAtUtc.HasValue || now >= x.StartsAtUtc.Value)
                        && (!x.EndsAtUtc.HasValue || now <= x.EndsAtUtc.Value)
                        && (!x.MaximumTotalUses.HasValue || x.UsedCount < x.MaximumTotalUses.Value)
                        && (!x.CampaignId.HasValue
                            || _db.CouponCampaigns.Any(c =>
                                c.Id == x.CampaignId.Value
                                && c.IsActive
                                && (!c.StartsAtUtc.HasValue || now >= c.StartsAtUtc.Value)
                                && (!c.EndsAtUtc.HasValue || now <= c.EndsAtUtc.Value))))
            .OrderBy(x => x.Code)
            .ToArrayAsync(cancellationToken);

        var result = coupons
            .Where(x => ConditionsMatch(x, "sales_channel", salesChannel))
            .Where(x => paymentMethod is null || ConditionsMatch(x, "payment_method", paymentMethod))
            .Where(x => shippingChannel is null || ConditionsMatch(x, "shipping_channel", shippingChannel))
            .Where(x => ProductScopeMatches(x, query))
            .ToArray();
        _cache.Set(cacheKey, result, BaseCouponCacheOptions);
        return result;
    }

    private static bool ConditionsMatch(Coupon coupon, string type, string value)
    {
        var conditions = coupon.Conditions.Where(x => x.Type == type).ToArray();
        return conditions.Length == 0 || conditions.Any(x => x.Value == value);
    }

    private static bool ProductScopeMatches(Coupon coupon, ShopCouponQuery query)
    {
        if (!HasProductFilter(query))
            return true;
        if (coupon.Scopes.Count == 0)
            return true;

        var sku = NormalizeSku(query.Sku);
        var categoryName = Normalize(query.CategoryName);

        return coupon.Scopes.Any(scope =>
            scope.Type switch
            {
                "product" => query.ProductId.HasValue && scope.ProductId == query.ProductId.Value,
                "variant" => (query.VariantId.HasValue && scope.VariantId == query.VariantId.Value)
                             || (sku is not null && string.Equals(scope.Sku, sku, StringComparison.OrdinalIgnoreCase)),
                "sku" => sku is not null && string.Equals(scope.Sku, sku, StringComparison.OrdinalIgnoreCase),
                "category" => (query.ZortCategoryId.HasValue && scope.ZortCategoryId == query.ZortCategoryId.Value)
                              || (categoryName is not null
                                  && string.Equals(
                                      Normalize(scope.CategoryName),
                                      categoryName,
                                      StringComparison.OrdinalIgnoreCase)),
                _ => false
            });
    }

    private static bool HasProductFilter(ShopCouponQuery query)
        => query.ProductId.HasValue
           || query.VariantId.HasValue
           || !string.IsNullOrWhiteSpace(query.Sku)
           || query.ZortCategoryId.HasValue
           || !string.IsNullOrWhiteSpace(query.CategoryName);

    private static bool IsCustomerScopeVisible(Coupon coupon, Guid customerId)
    {
        if (coupon.CustomerScopes.Count == 0)
            return true;

        return coupon.CustomerScopes.Any(scope =>
            scope.Type switch
            {
                "customer" => scope.CustomerId == customerId,
                "new_customer" or "existing_customer" or "first_order" => true,
                _ => false
            });
    }

    private static bool CanClaim(Coupon coupon, bool isClaimed, int customerUsageCount)
    {
        if (isClaimed)
            return false;
        if (coupon.MaximumTotalUses.HasValue && coupon.UsedCount >= coupon.MaximumTotalUses.Value)
            return false;
        if (coupon.MaximumUsesPerCustomer.HasValue && customerUsageCount >= coupon.MaximumUsesPerCustomer.Value)
            return false;

        return true;
    }

    private async Task<Dictionary<Guid, CouponClaim>> GetClaimsAsync(
        Guid customerId,
        IReadOnlyCollection<Guid> couponIds,
        CancellationToken cancellationToken)
    {
        if (couponIds.Count == 0)
            return [];

        return await _db.CouponClaims.AsNoTracking()
            .Where(x => x.CustomerId == customerId && couponIds.Contains(x.CouponId))
            .ToDictionaryAsync(x => x.CouponId, cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> GetUsageCountsAsync(
        Guid customerId,
        IReadOnlyCollection<Guid> couponIds,
        CancellationToken cancellationToken)
    {
        if (couponIds.Count == 0)
            return [];

        return await _db.CouponUsages.AsNoTracking()
            .Where(x => x.CustomerId == customerId
                        && !x.IsReleased
                        && couponIds.Contains(x.CouponId))
            .GroupBy(x => x.CouponId)
            .Select(x => new { CouponId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.CouponId, x => x.Count, cancellationToken);
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

    private static ShopCouponResponse ToResponse(
        Coupon coupon,
        CouponClaim? claim,
        bool canClaim,
        DateTime now)
    {
        var remainingTotalUses = coupon.MaximumTotalUses.HasValue
            ? Math.Max(0, coupon.MaximumTotalUses.Value - coupon.UsedCount)
            : (int?)null;

        return new ShopCouponResponse(
            coupon.Id,
            coupon.Code,
            coupon.Name,
            coupon.Description,
            coupon.Type,
            coupon.Value,
            coupon.MinimumSubtotal,
            coupon.MaximumDiscount,
            coupon.StartsAtUtc,
            coupon.EndsAtUtc,
            coupon.MaximumTotalUses,
            coupon.MaximumUsesPerCustomer,
            coupon.UsedCount,
            remainingTotalUses,
            claim is not null,
            canClaim && (!coupon.StartsAtUtc.HasValue || now >= coupon.StartsAtUtc.Value),
            claim?.ClaimedAtUtc,
            coupon.Scopes.Select(ToScopeLabel).ToArray(),
            coupon.Conditions.Select(x => $"{x.Type}:{x.Value}").ToArray());
    }

    private static string ToScopeLabel(CouponScope scope)
        => scope.Type switch
        {
            "product" => $"product:{scope.ProductId}",
            "variant" => scope.VariantId.HasValue ? $"variant:{scope.VariantId}" : $"sku:{scope.Sku}",
            "sku" => $"sku:{scope.Sku}",
            "category" => $"category:{scope.CategoryName}",
            _ => scope.Type
        };

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? NormalizeSku(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static readonly MemoryCacheEntryOptions BaseCouponCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20),
        SlidingExpiration = TimeSpan.FromSeconds(10),
        Size = 1
    };

    private static string BuildBaseCouponCacheKey(
        DateTime now,
        ShopCouponQuery query,
        string salesChannel,
        string? paymentMethod,
        string? shippingChannel)
    {
        var timeBucket = now.Ticks / TimeSpan.FromMinutes(1).Ticks;
        return string.Join(
            ':',
            "promotion:shop-coupons:base",
            timeBucket,
            salesChannel,
            paymentMethod ?? "-",
            shippingChannel ?? "-",
            query.ProductId?.ToString("N") ?? "-",
            query.VariantId?.ToString("N") ?? "-",
            NormalizeSku(query.Sku) ?? "-",
            query.ZortCategoryId?.ToString() ?? "-",
            Normalize(query.CategoryName) ?? "-");
    }
}
