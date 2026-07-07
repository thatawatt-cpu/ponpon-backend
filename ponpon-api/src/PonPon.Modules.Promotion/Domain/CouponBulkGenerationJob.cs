namespace PonPon.Modules.Promotion.Domain;

public sealed class CouponBulkGenerationJob
{
    private CouponBulkGenerationJob()
    {
        Status = CouponBulkGenerationJobStatus.Pending;
        Prefix = string.Empty;
        InputJson = "{}";
    }

    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public string Prefix { get; private set; }
    public int RequestedCount { get; private set; }
    public int CreatedCount { get; private set; }
    public string InputJson { get; private set; }
    public string Status { get; private set; }
    public string? BackgroundJobId { get; private set; }
    public string? Error { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public string? RequestedByUserType { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public static CouponBulkGenerationJob Queue(
        Guid batchId,
        Guid? campaignId,
        string prefix,
        int requestedCount,
        string inputJson,
        Guid? requestedByUserId,
        string? requestedByUserType,
        DateTime requestedAtUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            CampaignId = campaignId,
            Prefix = prefix,
            RequestedCount = requestedCount,
            InputJson = inputJson,
            RequestedByUserId = requestedByUserId,
            RequestedByUserType = requestedByUserType,
            RequestedAtUtc = requestedAtUtc
        };

    public void AttachBackgroundJob(string backgroundJobId)
        => BackgroundJobId = backgroundJobId;

    public void MarkRunning(DateTime startedAtUtc)
    {
        Status = CouponBulkGenerationJobStatus.Running;
        Error = null;
        StartedAtUtc ??= startedAtUtc;
        CompletedAtUtc = null;
    }

    public void MarkCompleted(int createdCount, DateTime completedAtUtc)
    {
        CreatedCount = createdCount;
        Status = CouponBulkGenerationJobStatus.Completed;
        Error = null;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkFailed(string error, DateTime completedAtUtc)
    {
        Status = CouponBulkGenerationJobStatus.Failed;
        Error = error.Length <= 2000 ? error : error[..2000];
        CompletedAtUtc = completedAtUtc;
    }
}

public static class CouponBulkGenerationJobStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
