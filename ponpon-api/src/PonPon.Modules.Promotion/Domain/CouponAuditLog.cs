namespace PonPon.Modules.Promotion.Domain;

public sealed class CouponAuditLog
{
    private CouponAuditLog()
    {
        Action = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid? CouponId { get; private set; }
    public Guid? BatchId { get; private set; }
    public string Action { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorUserType { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static CouponAuditLog Create(
        Guid? couponId,
        Guid? batchId,
        string action,
        Guid? actorUserId,
        string? actorUserType,
        string? beforeJson,
        string? afterJson,
        DateTime now)
    {
        return new CouponAuditLog
        {
            Id = Guid.NewGuid(),
            CouponId = couponId,
            BatchId = batchId,
            Action = action.Trim().ToLowerInvariant(),
            ActorUserId = actorUserId,
            ActorUserType = string.IsNullOrWhiteSpace(actorUserType) ? null : actorUserType.Trim(),
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            CreatedAtUtc = now
        };
    }
}
