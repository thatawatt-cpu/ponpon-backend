using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Promotion.Domain;
using PonPon.Modules.Promotion.Infrastructure;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Promotion.Application;

public sealed class CouponService : ICouponService
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly HashSet<string> SupportedPaymentMethods =
    [
        "promptpay",
        "card",
        "mobile_banking_bbl",
        "mobile_banking_kbank",
        "mobile_banking_scb",
        "mobile_banking_ktb",
        "mobile_banking_bay"
    ];
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PromotionDbContext _db;
    private readonly ICurrentUser? _currentUser;

    public CouponService(PromotionDbContext db, ICurrentUser? currentUser = null)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return _db.Coupons.AsNoTracking()
            .Include(x => x.Scopes)
            .Include(x => x.CustomerScopes)
            .Include(x => x.Conditions)
            .FirstOrDefaultAsync(
                x => x.Code == code.Trim().ToUpper()
                     && (!x.CampaignId.HasValue
                         || _db.CouponCampaigns.Any(c =>
                             c.Id == x.CampaignId.Value
                             && c.IsActive
                             && (!c.StartsAtUtc.HasValue || now >= c.StartsAtUtc.Value)
                             && (!c.EndsAtUtc.HasValue || now <= c.EndsAtUtc.Value))),
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Coupon>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Coupons.AsNoTracking()
            .Include(x => x.Scopes)
            .Include(x => x.CustomerScopes)
            .Include(x => x.Conditions)
            .OrderBy(x => x.Code)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Coupon>> GetByCampaignAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default)
        => await _db.Coupons.AsNoTracking()
            .Include(x => x.Scopes)
            .Include(x => x.CustomerScopes)
            .Include(x => x.Conditions)
            .Where(x => x.CampaignId == campaignId)
            .OrderBy(x => x.Code)
            .ToArrayAsync(cancellationToken);

    public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Coupons.AsNoTracking()
            .Include(x => x.Scopes)
            .Include(x => x.CustomerScopes)
            .Include(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<CouponUsage>> GetUsagesAsync(
        Guid couponId, CancellationToken cancellationToken = default)
        => await _db.CouponUsages.AsNoTracking()
            .Where(x => x.CouponId == couponId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CouponAuditLog>> GetAuditLogsAsync(
        Guid couponId,
        CancellationToken cancellationToken = default)
        => await _db.CouponAuditLogs.AsNoTracking()
            .Where(x => x.CouponId == couponId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<int> GetActiveCustomerUsageCountAsync(
        Guid couponId, Guid customerId, CancellationToken cancellationToken = default)
        => _db.CouponUsages.CountAsync(
            x => x.CouponId == couponId && x.CustomerId == customerId && !x.IsReleased,
            cancellationToken);

    public async Task<Guid> CreateAsync(CouponInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);
        await EnsureCampaignExistsAsync(input.CampaignId, cancellationToken);
        var code = input.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(x => x.Code == code, cancellationToken))
            throw new BadRequestException("Coupon code already exists.");

        var coupon = Coupon.Create(input, DateTime.UtcNow);
        await _db.Coupons.AddAsync(coupon, cancellationToken);
        _db.CouponAuditLogs.Add(CreateAuditLog(
            coupon.Id,
            null,
            "created",
            null,
            ToAuditJson(coupon),
            DateTime.UtcNow));
        await _db.SaveChangesAsync(cancellationToken);
        return coupon.Id;
    }

    public async Task<CouponBulkGenerateResult> BulkGenerateAsync(
        CouponBulkGenerateInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateBulk(input);
        await EnsureCampaignExistsAsync(input.CampaignId, cancellationToken);
        var batchId = input.BatchId ?? Guid.NewGuid();
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var batchLockKey = BitConverter.ToInt64(batchId.ToByteArray(), 0);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({batchLockKey})",
            cancellationToken);
        var existingCodes = await GetBatchCodesAsync(batchId, cancellationToken);
        if (existingCodes.Count > 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new CouponBulkGenerateResult(
                batchId,
                input.CampaignId,
                existingCodes.Count,
                existingCodes);
        }

        var now = DateTime.UtcNow;
        var prefix = NormalizePrefix(input.Prefix);
        var codes = await GenerateUniqueCodesAsync(prefix, input.CodeLength, input.Count, cancellationToken);
        var coupons = codes
            .Select(code => Coupon.Create(ToCouponInput(code, input.Template, input.CampaignId), now))
            .ToArray();

        await _db.Coupons.AddRangeAsync(coupons, cancellationToken);
        foreach (var coupon in coupons)
        {
            _db.CouponAuditLogs.Add(CreateAuditLog(
                coupon.Id,
                batchId,
                "bulk_generated",
                null,
                ToAuditJson(coupon),
                now,
                input.ActorUserId,
                input.ActorUserType));
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CouponBulkGenerateResult(
            batchId,
            input.CampaignId,
            coupons.Length,
            coupons.Select(x => x.Code).ToArray());
    }

    public async Task UpdateAsync(Guid id, CouponInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);
        await EnsureCampaignExistsAsync(input.CampaignId, cancellationToken);
        var coupon = await _db.Coupons
            .Include(x => x.Scopes)
            .Include(x => x.CustomerScopes)
            .Include(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coupon was not found.");
        var code = input.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            throw new BadRequestException("Coupon code already exists.");

        var beforeJson = ToAuditJson(coupon);
        coupon.Update(input, DateTime.UtcNow);
        _db.CouponAuditLogs.Add(CreateAuditLog(
            coupon.Id,
            null,
            "updated",
            beforeJson,
            ToAuditJson(coupon),
            DateTime.UtcNow));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coupon was not found.");
        var beforeJson = ToAuditJson(coupon);
        if (await _db.CouponUsages.AnyAsync(x => x.CouponId == id, cancellationToken))
        {
            coupon.Deactivate(DateTime.UtcNow);
            _db.CouponAuditLogs.Add(CreateAuditLog(
                coupon.Id,
                null,
                "deactivated",
                beforeJson,
                ToAuditJson(coupon),
                DateTime.UtcNow));
        }
        else
        {
            _db.Coupons.Remove(coupon);
            _db.CouponAuditLogs.Add(CreateAuditLog(
                coupon.Id,
                null,
                "deleted",
                beforeJson,
                null,
                DateTime.UtcNow));
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryReserveAsync(
        Guid couponId,
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default,
        decimal discountAmount = 0)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var orderGlobalKey = BitConverter.ToInt64(orderId.ToByteArray(), 0);
        var couponGlobalKey = BitConverter.ToInt64(couponId.ToByteArray(), 0);
        var couponKey = BitConverter.ToInt32(couponId.ToByteArray(), 0);
        var customerKey = BitConverter.ToInt32(customerId.ToByteArray(), 0);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({orderGlobalKey})",
            cancellationToken);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({couponGlobalKey})",
            cancellationToken);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({couponKey}, {customerKey})",
            cancellationToken);

        var existing = await _db.CouponUsages
            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.CouponId == couponId, cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existing.CustomerId == customerId
                && !existing.IsReleased;
        }

        var now = DateTime.UtcNow;
        var coupon = await _db.Coupons.FirstOrDefaultAsync(
            x => x.Id == couponId
                 && (!x.CampaignId.HasValue
                     || _db.CouponCampaigns.Any(c =>
                         c.Id == x.CampaignId.Value
                         && c.IsActive
                         && (!c.StartsAtUtc.HasValue || now >= c.StartsAtUtc.Value)
                         && (!c.EndsAtUtc.HasValue || now <= c.EndsAtUtc.Value))),
            cancellationToken);
        if (coupon is null
            || !coupon.IsActive
            || (coupon.StartsAtUtc.HasValue && now < coupon.StartsAtUtc.Value)
            || (coupon.EndsAtUtc.HasValue && now > coupon.EndsAtUtc.Value)
            || (coupon.MaximumTotalUses.HasValue && coupon.UsedCount >= coupon.MaximumTotalUses.Value))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (coupon.MaximumUsesPerCustomer.HasValue)
        {
            var customerUses = await _db.CouponUsages.CountAsync(
                x => x.CouponId == couponId
                     && x.CustomerId == customerId
                     && !x.IsReleased,
                cancellationToken);
            if (customerUses >= coupon.MaximumUsesPerCustomer.Value)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }
        }

        var activeOrderCoupons = await _db.CouponUsages
            .Where(x => x.OrderId == orderId && !x.IsReleased)
            .Join(
                _db.Coupons,
                usage => usage.CouponId,
                existingCoupon => existingCoupon.Id,
                (_, existingCoupon) => existingCoupon.Type)
            .ToArrayAsync(cancellationToken);
        if (activeOrderCoupons.Length >= 2)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
        var isFreeShipping = coupon.Type == "free_shipping";
        if (isFreeShipping && activeOrderCoupons.Any(x => x == "free_shipping"))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
        if (!isFreeShipping && activeOrderCoupons.Any(x => x != "free_shipping"))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        coupon.IncrementUsage();
        await _db.CouponUsages.AddAsync(
            CouponUsage.Reserve(couponId, orderId, customerId, discountAmount, now),
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var usages = await _db.CouponUsages
            .Where(x => x.OrderId == orderId && !x.IsReleased)
            .ToArrayAsync(cancellationToken);
        if (usages.Length == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        foreach (var couponId in usages.Select(x => x.CouponId).Distinct())
        {
            var couponGlobalKey = BitConverter.ToInt64(couponId.ToByteArray(), 0);
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({couponGlobalKey})",
                cancellationToken);
        }
        foreach (var usage in usages)
            await _db.Entry(usage).ReloadAsync(cancellationToken);

        var activeUsages = usages.Where(x => !x.IsReleased).ToArray();
        var couponIds = activeUsages.Select(x => x.CouponId).Distinct().ToArray();
        var coupons = await _db.Coupons
            .Where(x => couponIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        foreach (var usage in activeUsages)
        {
            usage.Release(DateTime.UtcNow);
            if (coupons.TryGetValue(usage.CouponId, out var coupon))
                coupon.DecrementUsage();
        }
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void Validate(CouponInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || input.Code.Trim().Length > 64)
            throw new BadRequestException("Coupon code is required and must not exceed 64 characters.");
        var couponType = input.Type.Trim().ToLowerInvariant();
        if (couponType is not ("fixed" or "percentage" or "free_shipping"))
            throw new BadRequestException("Coupon type must be fixed, percentage, or free_shipping.");
        if ((couponType == "free_shipping" && input.Value != 0)
            || (couponType != "free_shipping"
                && (input.Value <= 0 || (couponType == "percentage" && input.Value > 100))))
            throw new BadRequestException("Coupon value is invalid.");
        if (input.MinimumSubtotal < 0 || input.MaximumDiscount is <= 0)
            throw new BadRequestException("Coupon minimum subtotal or maximum discount is invalid.");
        if (input.StartsAtUtc.HasValue && input.EndsAtUtc.HasValue
            && input.StartsAtUtc.Value >= input.EndsAtUtc.Value)
            throw new BadRequestException("Coupon start date must be earlier than end date.");
        if (input.MaximumTotalUses is <= 0 || input.MaximumUsesPerCustomer is <= 0)
            throw new BadRequestException("Coupon usage limits must be greater than zero.");
        foreach (var scope in input.Scopes)
        {
            var type = scope.Type.Trim().ToLowerInvariant();
            if (type is not ("product" or "variant" or "sku" or "category"))
                throw new BadRequestException("Coupon scope type must be product, variant, sku, or category.");
            if (type == "product" && scope.ProductId is null)
                throw new BadRequestException("Product coupon scope requires productId.");
            if (type == "variant" && scope.VariantId is null && string.IsNullOrWhiteSpace(scope.Sku))
                throw new BadRequestException("Variant coupon scope requires variantId or sku.");
            if (type == "sku" && string.IsNullOrWhiteSpace(scope.Sku))
                throw new BadRequestException("SKU coupon scope requires sku.");
            if (type == "category" && string.IsNullOrWhiteSpace(scope.CategoryName))
                throw new BadRequestException("Category coupon scope requires categoryName.");
        }
        foreach (var scope in input.CustomerScopes)
        {
            var type = scope.Type.Trim().ToLowerInvariant();
            if (type is not ("new_customer" or "existing_customer" or "first_order" or "customer"))
                throw new BadRequestException("Coupon customer scope type must be new_customer, existing_customer, first_order, or customer.");
            if (type == "customer" && scope.CustomerId is null)
                throw new BadRequestException("Customer coupon scope requires customerId.");
        }
        foreach (var condition in input.Conditions)
        {
            var type = condition.Type.Trim().ToLowerInvariant();
            if (type is not ("sales_channel" or "payment_method" or "shipping_channel"))
                throw new BadRequestException(
                    "Coupon condition type must be sales_channel, payment_method, or shipping_channel.");
            if (string.IsNullOrWhiteSpace(condition.Value) || condition.Value.Trim().Length > 128)
                throw new BadRequestException(
                    "Coupon condition value is required and must not exceed 128 characters.");
            if (type == "payment_method"
                && !SupportedPaymentMethods.Contains(condition.Value.Trim().ToLowerInvariant()))
                throw new BadRequestException("Coupon payment method is not supported.");
        }
        if (input.Conditions
            .GroupBy(x => new
            {
                Type = x.Type.Trim().ToLowerInvariant(),
                Value = x.Value.Trim().ToLowerInvariant()
            })
            .Any(x => x.Count() > 1))
            throw new BadRequestException("Coupon conditions must not contain duplicates.");
    }

    private async Task<IReadOnlyCollection<string>> GenerateUniqueCodesAsync(
        string prefix,
        int codeLength,
        int count,
        CancellationToken cancellationToken)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var attempts = 0;
        while (codes.Count < count)
        {
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var targetSize = Math.Min(1000, count - codes.Count);
            while (candidates.Count < targetSize)
            {
                attempts++;
                if (attempts > count * 20)
                    throw new BadRequestException("Unable to generate enough unique coupon codes.");

                var code = string.IsNullOrWhiteSpace(prefix)
                    ? GenerateCode(codeLength)
                    : $"{prefix}-{GenerateCode(codeLength)}";
                if (!codes.Contains(code))
                    candidates.Add(code);
            }

            var candidateArray = candidates.ToArray();
            var existing = await _db.Coupons.AsNoTracking()
                .Where(x => candidateArray.Contains(x.Code))
                .Select(x => x.Code)
                .ToArrayAsync(cancellationToken);
            candidates.ExceptWith(existing);
            codes.UnionWith(candidates);
        }

        return codes.Take(count).ToArray();
    }

    private static string GenerateCode(int length)
    {
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            builder.Append(CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)]);
        }

        return builder.ToString();
    }

    private static CouponInput ToCouponInput(
        string code,
        CouponTemplateInput template,
        Guid? campaignId = null)
    {
        return new CouponInput(
            code,
            template.Type,
            template.Value,
            template.MinimumSubtotal,
            template.MaximumDiscount,
            template.StartsAtUtc,
            template.EndsAtUtc,
            template.CanCombineWithFlashSale,
            template.MaximumTotalUses,
            template.MaximumUsesPerCustomer,
            template.IsActive,
            template.Scopes,
            template.CustomerScopes,
            template.Conditions,
            template.CanStackWithPromotions,
            template.CanStackWithCoupons,
            campaignId);
    }

    internal static void ValidateBulkInput(CouponBulkGenerateInput input)
        => ValidateBulk(input);

    private static void ValidateBulk(CouponBulkGenerateInput input)
    {
        if (input.Count is <= 0 or > 20000)
            throw new BadRequestException("Bulk coupon count must be between 1 and 20000.");
        if (input.CodeLength is < 4 or > 32)
            throw new BadRequestException("Bulk coupon codeLength must be between 4 and 32.");
        if (NormalizePrefix(input.Prefix).Length > 32)
            throw new BadRequestException("Bulk coupon prefix must not exceed 32 characters.");

        Validate(ToCouponInput("BULK-TEMPLATE", input.Template));
    }

    private static string NormalizePrefix(string prefix)
    {
        return string.IsNullOrWhiteSpace(prefix)
            ? string.Empty
            : prefix.Trim().ToUpperInvariant().Replace(' ', '-');
    }

    private CouponAuditLog CreateAuditLog(
        Guid? couponId,
        Guid? batchId,
        string action,
        string? beforeJson,
        string? afterJson,
        DateTime now,
        Guid? actorUserId = null,
        string? actorUserType = null)
    {
        return CouponAuditLog.Create(
            couponId,
            batchId,
            action,
            actorUserId ?? _currentUser?.UserId,
            actorUserType ?? _currentUser?.UserType,
            beforeJson,
            afterJson,
            now);
    }

    private async Task<IReadOnlyCollection<string>> GetBatchCodesAsync(
        Guid batchId,
        CancellationToken cancellationToken)
        => await _db.CouponAuditLogs.AsNoTracking()
            .Where(x => x.BatchId == batchId && x.CouponId.HasValue)
            .Join(
                _db.Coupons.AsNoTracking(),
                log => log.CouponId!.Value,
                coupon => coupon.Id,
                (_, coupon) => coupon.Code)
            .OrderBy(x => x)
            .ToArrayAsync(cancellationToken);

    private static string ToAuditJson(Coupon coupon)
    {
        var snapshot = new
        {
            coupon.Id,
            coupon.CampaignId,
            coupon.Code,
            coupon.Type,
            coupon.Value,
            coupon.MinimumSubtotal,
            coupon.MaximumDiscount,
            coupon.StartsAtUtc,
            coupon.EndsAtUtc,
            coupon.CanCombineWithFlashSale,
            coupon.CanStackWithPromotions,
            coupon.CanStackWithCoupons,
            coupon.MaximumTotalUses,
            coupon.MaximumUsesPerCustomer,
            coupon.UsedCount,
            coupon.IsActive,
            Scopes = coupon.Scopes.Select(x => new
            {
                x.Type,
                x.ProductId,
                x.VariantId,
                x.Sku,
                x.CategoryName
            }),
            CustomerScopes = coupon.CustomerScopes.Select(x => new
            {
                x.Type,
                x.CustomerId
            }),
            Conditions = coupon.Conditions.Select(x => new
            {
                x.Type,
                x.Value
            })
        };

        return JsonSerializer.Serialize(snapshot, AuditJsonOptions);
    }

    private async Task EnsureCampaignExistsAsync(
        Guid? campaignId,
        CancellationToken cancellationToken)
    {
        if (campaignId.HasValue
            && !await _db.CouponCampaigns.AnyAsync(
                x => x.Id == campaignId.Value && x.IsActive,
                cancellationToken))
            throw new BadRequestException("Coupon campaign was not found or is inactive.");
    }
}
