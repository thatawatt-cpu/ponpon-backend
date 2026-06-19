using PonPon.Shared.Domain;

namespace PonPon.Modules.Identity.Domain.RefreshTokens;

public sealed class RefreshToken : Entity, IAuditableEntity
{
    private RefreshToken()
    {
        TokenHash = string.Empty;
        UserType = string.Empty;
    }

    public RefreshToken(string tokenHash, Guid subjectId, string userType, DateTime expiresAtUtc, DateTime nowUtc)
    {
        TokenHash = tokenHash;
        SubjectId = subjectId;
        UserType = userType;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = nowUtc;
    }

    public string TokenHash { get; private set; }
    public Guid SubjectId { get; private set; }
    public string UserType { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    public void Revoke(DateTime nowUtc)
    {
        RevokedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }
}
