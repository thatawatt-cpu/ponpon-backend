namespace PonPon.Modules.Identity.Domain.Users;

public sealed class AdminUserAuditLog
{
    private AdminUserAuditLog()
    {
        Action = string.Empty;
        DetailsJson = "{}";
    }

    public AdminUserAuditLog(
        Guid? actorUserId,
        Guid targetUserId,
        string action,
        string detailsJson,
        DateTime nowUtc)
    {
        Id = Guid.NewGuid();
        ActorUserId = actorUserId;
        TargetUserId = targetUserId;
        Action = action.Trim();
        DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? "{}" : detailsJson;
        CreatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public Guid TargetUserId { get; private set; }
    public string Action { get; private set; }
    public string DetailsJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
