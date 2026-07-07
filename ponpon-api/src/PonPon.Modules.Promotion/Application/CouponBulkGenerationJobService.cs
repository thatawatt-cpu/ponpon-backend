using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Promotion.Domain;
using PonPon.Modules.Promotion.Infrastructure;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Promotion.Application;

public interface ICouponBulkGenerationJobService
{
    Task<CouponBulkGenerationJob> QueueAsync(
        CouponBulkGenerateInput input,
        CancellationToken cancellationToken = default);
    Task<CouponBulkGenerationJob?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CouponBulkGenerationJob>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetCodesAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task AttachBackgroundJobAsync(
        Guid id,
        string backgroundJobId,
        CancellationToken cancellationToken = default);
    Task MarkEnqueueFailedAsync(
        Guid id,
        string error,
        CancellationToken cancellationToken = default);
    Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class CouponBulkGenerationJobService : ICouponBulkGenerationJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PromotionDbContext _db;
    private readonly ICouponService _coupons;
    private readonly ICurrentUser? _currentUser;

    public CouponBulkGenerationJobService(
        PromotionDbContext db,
        ICouponService coupons,
        ICurrentUser? currentUser = null)
    {
        _db = db;
        _coupons = coupons;
        _currentUser = currentUser;
    }

    public async Task<CouponBulkGenerationJob> QueueAsync(
        CouponBulkGenerateInput input,
        CancellationToken cancellationToken = default)
    {
        CouponService.ValidateBulkInput(input);
        if (input.CampaignId.HasValue
            && !await _db.CouponCampaigns.AnyAsync(x => x.Id == input.CampaignId.Value, cancellationToken))
            throw new NotFoundException("Coupon campaign was not found.");

        var batchId = Guid.NewGuid();
        var enrichedInput = input with
        {
            BatchId = batchId,
            ActorUserId = _currentUser?.UserId,
            ActorUserType = _currentUser?.UserType
        };
        var job = CouponBulkGenerationJob.Queue(
            batchId,
            input.CampaignId,
            input.Prefix,
            input.Count,
            JsonSerializer.Serialize(enrichedInput, JsonOptions),
            _currentUser?.UserId,
            _currentUser?.UserType,
            DateTime.UtcNow);
        _db.CouponBulkGenerationJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        return job;
    }

    public Task<CouponBulkGenerationJob?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _db.CouponBulkGenerationJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<CouponBulkGenerationJob>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
        => await _db.CouponBulkGenerationJobs.AsNoTracking()
            .OrderByDescending(x => x.RequestedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetCodesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var job = await _db.CouponBulkGenerationJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coupon bulk generation job was not found.");
        if (job.Status != CouponBulkGenerationJobStatus.Completed)
            throw new BadRequestException("Coupon bulk generation job is not completed.");

        return await GetCodesByBatchAsync(job.BatchId, cancellationToken);
    }

    public async Task AttachBackgroundJobAsync(
        Guid id,
        string backgroundJobId,
        CancellationToken cancellationToken = default)
    {
        var job = await FindRequiredAsync(id, cancellationToken);
        job.AttachBackgroundJob(backgroundJobId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkEnqueueFailedAsync(
        Guid id,
        string error,
        CancellationToken cancellationToken = default)
    {
        var job = await FindRequiredAsync(id, cancellationToken);
        job.MarkFailed(error, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await FindRequiredAsync(id, cancellationToken);
        if (job.Status == CouponBulkGenerationJobStatus.Completed)
            return;

        job.MarkRunning(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var input = JsonSerializer.Deserialize<CouponBulkGenerateInput>(job.InputJson, JsonOptions)
                ?? throw new InvalidOperationException("Coupon bulk generation input is invalid.");
            var result = await _coupons.BulkGenerateAsync(input, cancellationToken);
            job.MarkCompleted(result.CreatedCount, DateTime.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            job.MarkFailed(ex.Message, DateTime.UtcNow);
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<CouponBulkGenerationJob> FindRequiredAsync(
        Guid id,
        CancellationToken cancellationToken)
        => await _db.CouponBulkGenerationJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
           ?? throw new NotFoundException("Coupon bulk generation job was not found.");

    private async Task<IReadOnlyCollection<string>> GetCodesByBatchAsync(
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
}

public sealed class CouponBulkGenerationBackgroundJob
{
    private readonly ICouponBulkGenerationJobService _jobs;

    public CouponBulkGenerationBackgroundJob(ICouponBulkGenerationJobService jobs)
    {
        _jobs = jobs;
    }

    public Task ExecuteAsync(Guid jobId)
        => _jobs.ExecuteAsync(jobId, CancellationToken.None);
}
