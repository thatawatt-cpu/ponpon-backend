using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Promotion.Infrastructure;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Promotion.Application;

public sealed class CouponProfileSummaryProvider : ICustomerProfileSummaryProvider
{
    private readonly PromotionDbContext _db;

    public CouponProfileSummaryProvider(PromotionDbContext db)
    {
        _db = db;
    }

    public async Task<CustomerProfileSummaryCounts> GetCountsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var couponCount = await _db.CouponClaims.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId, cancellationToken);

        return new CustomerProfileSummaryCounts(CouponCount: couponCount);
    }
}
