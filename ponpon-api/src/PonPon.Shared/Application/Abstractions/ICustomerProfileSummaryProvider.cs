namespace PonPon.Shared.Application.Abstractions;

public interface ICustomerProfileSummaryProvider
{
    Task<CustomerProfileSummaryCounts> GetCountsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerProfileSummaryCounts(
    int? WishlistCount = null,
    int? CouponCount = null,
    int? RecentlyViewedCount = null);
