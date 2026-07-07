using System.Text.Json;
using PonPon.Shared.Domain;

namespace PonPon.Modules.Ordering.Domain.SyncRuns;

public sealed class OrderSyncRun : Entity
{
    private OrderSyncRun()
    {
        Status = OrderSyncRunStatus.Pending;
    }

    private OrderSyncRun(DateTime requestedAtUtc) : this()
    {
        RequestedAtUtc = requestedAtUtc;
    }

    public string Status { get; private set; }
    public string? BackgroundJobId { get; private set; }
    public int TotalFetched { get; private set; }
    public int Created { get; private set; }
    public int Updated { get; private set; }
    public int Failed { get; private set; }
    public string ErrorsJson { get; private set; } = "[]";
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public static OrderSyncRun Queue(DateTime requestedAtUtc) => new(requestedAtUtc);

    public void AttachBackgroundJob(string backgroundJobId)
    {
        BackgroundJobId = backgroundJobId;
    }

    public void MarkRunning(DateTime startedAtUtc)
    {
        Status = OrderSyncRunStatus.Running;
        StartedAtUtc = startedAtUtc;
    }

    public void MarkCompleted(
        int totalFetched,
        int created,
        int updated,
        int failed,
        IReadOnlyCollection<string> errors,
        DateTime completedAtUtc)
    {
        TotalFetched = totalFetched;
        Created = created;
        Updated = updated;
        Failed = failed;
        ErrorsJson = JsonSerializer.Serialize(errors);
        Status = failed > 0 ? OrderSyncRunStatus.CompletedWithErrors : OrderSyncRunStatus.Succeeded;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkFailed(string error, DateTime completedAtUtc)
    {
        Failed = Math.Max(Failed, 1);
        ErrorsJson = JsonSerializer.Serialize(new[] { error });
        Status = OrderSyncRunStatus.Failed;
        CompletedAtUtc = completedAtUtc;
    }
}

public static class OrderSyncRunStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string CompletedWithErrors = "CompletedWithErrors";
    public const string Failed = "Failed";
}
