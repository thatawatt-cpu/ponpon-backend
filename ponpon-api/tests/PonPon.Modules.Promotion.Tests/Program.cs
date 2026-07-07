using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Domain;
using PonPon.Modules.Promotion.Infrastructure;

var bulkJob = CouponBulkGenerationJob.Queue(
    Guid.NewGuid(),
    null,
    "BULK",
    10_000,
    "{}",
    Guid.NewGuid(),
    "admin",
    DateTime.UtcNow);
if (bulkJob.Status != CouponBulkGenerationJobStatus.Pending)
    throw new InvalidOperationException("Bulk generation job must start as pending.");
bulkJob.AttachBackgroundJob("hangfire-1");
bulkJob.MarkRunning(DateTime.UtcNow);
bulkJob.MarkCompleted(10_000, DateTime.UtcNow);
if (bulkJob.Status != CouponBulkGenerationJobStatus.Completed
    || bulkJob.CreatedCount != 10_000
    || bulkJob.BackgroundJobId != "hangfire-1")
    throw new InvalidOperationException("Bulk generation job state transition failed.");
Console.WriteLine("PASS CouponBulkGenerationJobTransitions");

var connection = Environment.GetEnvironmentVariable("PONPON_INTEGRATION_DB");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.WriteLine("SKIP CouponQuotaConcurrency (set PONPON_INTEGRATION_DB to run PostgreSQL integration tests)");
    return;
}

var options = new DbContextOptionsBuilder<PromotionDbContext>()
    .UseNpgsql(connection, x => x.MigrationsHistoryTable("__ef_migrations_history", PromotionDbContext.Schema))
    .Options;
await using (var setup = new PromotionDbContext(options))
{
    await setup.Database.MigrateAsync();
}

var input = new CouponInput(
    $"RACE-{Guid.NewGuid():N}", "fixed", 10, 0, null, null, null,
    true, 5, 1, true);
Guid couponId;
await using (var db = new PromotionDbContext(options))
    couponId = await new CouponService(db).CreateAsync(input);

try
{
    var attempts = Enumerable.Range(0, 20).Select(async _ =>
    {
        await using var db = new PromotionDbContext(options);
        return await new CouponService(db).TryReserveAsync(
            couponId, Guid.NewGuid(), Guid.NewGuid());
    });
    var accepted = (await Task.WhenAll(attempts)).Count(x => x);
    if (accepted != 5)
        throw new InvalidOperationException($"Expected exactly 5 accepted reservations, got {accepted}.");

    await using var verify = new PromotionDbContext(options);
    var coupon = await verify.Coupons.SingleAsync(x => x.Id == couponId);
    var usages = await verify.CouponUsages.CountAsync(x => x.CouponId == couponId && !x.IsReleased);
    if (coupon.UsedCount != 5 || usages != 5)
        throw new InvalidOperationException($"Quota state mismatch: UsedCount={coupon.UsedCount}, usages={usages}.");
    Console.WriteLine("PASS CouponQuotaConcurrency");
}
finally
{
    await using var cleanup = new PromotionDbContext(options);
    await cleanup.CouponUsages.Where(x => x.CouponId == couponId).ExecuteDeleteAsync();
    await cleanup.Coupons.Where(x => x.Id == couponId).ExecuteDeleteAsync();
}
