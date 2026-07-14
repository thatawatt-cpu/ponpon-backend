using PonPon.Shared.Domain;

namespace PonPon.Modules.Identity.Domain.Users;

public sealed class User : AggregateRoot, IAuditableEntity
{
    private readonly List<UserRole> _userRoles = [];

    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        DisplayName = string.Empty;
        PermissionsJson = "[]";
    }

    public User(string email, string passwordHash, string displayName, string permissionsJson, DateTime nowUtc)
    {
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        DisplayName = displayName;
        PermissionsJson = string.IsNullOrWhiteSpace(permissionsJson) ? "[]" : permissionsJson;
        Status = UserStatus.Active;
        CreatedAtUtc = nowUtc;
    }

    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string DisplayName { get; private set; }
    public string PermissionsJson { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public void MarkLoggedIn(DateTime nowUtc)
    {
        LastLoginAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateProfile(string displayName, string permissionsJson, UserStatus status, DateTime nowUtc)
    {
        DisplayName = displayName.Trim();
        PermissionsJson = string.IsNullOrWhiteSpace(permissionsJson) ? "[]" : permissionsJson;
        Status = status;
        UpdatedAtUtc = nowUtc;
    }

    public void ResetPassword(string passwordHash, DateTime nowUtc)
    {
        PasswordHash = passwordHash;
        UpdatedAtUtc = nowUtc;
    }
}
