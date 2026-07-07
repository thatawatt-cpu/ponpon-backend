using System.Text.Json;
using PonPon.Shared.Domain;

namespace PonPon.Modules.Catalog.Domain.SyncRuns;

public sealed class ProductSyncRun : Entity
{
    private ProductSyncRun()
    {
        Status = ProductSyncRunStatus.Pending;
    }

    private ProductSyncRun(DateTime requestedAtUtc) : this()
    {
        RequestedAtUtc = requestedAtUtc;
    }

    public string Status { get; private set; }
    public string? BackgroundJobId { get; private set; }
    public int TotalFetched { get; private set; }
    public int Created { get; private set; }
    public int Updated { get; private set; }
    public int Unchanged { get; private set; }
    public int Deactivated { get; private set; }
    public int Failed { get; private set; }
    public string ErrorsJson { get; private set; } = "[]";
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public static ProductSyncRun Queue(DateTime requestedAtUtc) => new(requestedAtUtc);

    public void AttachBackgroundJob(string backgroundJobId)
    {
        BackgroundJobId = backgroundJobId;
    }

    public void MarkRunning(DateTime startedAtUtc)
    {
        Status = ProductSyncRunStatus.Running;
        StartedAtUtc = startedAtUtc;
    }

    public void MarkCompleted(
        int totalFetched,
        int created,
        int updated,
        int unchanged,
        int deactivated,
        int failed,
        IReadOnlyCollection<string> errors,
        DateTime completedAtUtc)
    {
        TotalFetched = totalFetched;
        Created = created;
        Updated = updated;
        Unchanged = unchanged;
        Deactivated = deactivated;
        Failed = failed;
        ErrorsJson = JsonSerializer.Serialize(errors);
        Status = failed > 0 ? ProductSyncRunStatus.CompletedWithErrors : ProductSyncRunStatus.Succeeded;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkFailed(string error, DateTime completedAtUtc)
    {
        Failed = Math.Max(Failed, 1);
        ErrorsJson = JsonSerializer.Serialize(new[] { error });
        Status = ProductSyncRunStatus.Failed;
        CompletedAtUtc = completedAtUtc;
    }
}

public static class ProductSyncRunStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string CompletedWithErrors = "CompletedWithErrors";
    public const string Failed = "Failed";
}
