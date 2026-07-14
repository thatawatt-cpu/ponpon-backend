using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.Users;
using PonPon.Modules.Identity.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.AdminUsers;

public sealed class AdminUserManagementService
{
    public const string OwnerRole = "Owner";
    public const string AdminRole = "Admin";
    public const string StaffRole = "Staff";

    private static readonly string[] SupportedRoles = [OwnerRole, AdminRole, StaffRole];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IdentityDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;

    public AdminUserManagementService(
        IdentityDbContext db,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<AdminAuthMeResponse> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var actor = await GetCurrentAdminAsync(cancellationToken);
        return new AdminAuthMeResponse(
            actor.User.Id,
            actor.User.Email,
            actor.User.DisplayName,
            actor.Role,
            actor.Permissions);
    }

    public async Task<AdminUserListResponse> GetUsersAsync(
        string? search,
        UserStatus? status,
        string? role,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionAsync(AdminUserPermissions.AdminUsersRead, cancellationToken);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Email.ToLower().Contains(keyword)
                || x.DisplayName.ToLower().Contains(keyword));
        }

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = NormalizeRole(role);
            query = query.Where(x => x.UserRoles.Any(ur => ur.Role.Name == normalizedRole));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(x => x.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new AdminUserListResponse(
            users.Select(ToResponse).ToArray(),
            page,
            pageSize,
            total);
    }

    public async Task<Guid> CreateAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await EnsurePermissionAsync(AdminUserPermissions.AdminUsersManage, cancellationToken);
        var role = NormalizeRole(request.Role);
        if (!IsOwner(actor) && role == OwnerRole)
            throw PermissionDenied();

        ValidateEmail(request.Email);
        ValidatePassword(request.Password);
        ValidateDisplayName(request.DisplayName);
        var permissions = NormalizePermissions(role, request.Permissions ?? DefaultPermissionsForRole(role));
        var email = NormalizeEmail(request.Email);
        if (await _db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new BadRequestException("Email is already registered.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var roleEntity = await EnsureRoleAsync(role, cancellationToken);
        var now = _clock.UtcNow;
        var user = new User(
            email,
            _passwordHasher.Hash(request.Password),
            request.DisplayName.Trim(),
            SerializePermissions(permissions),
            now);

        await _db.Users.AddAsync(user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _db.UserRoles.AddAsync(new UserRole(user.Id, roleEntity.Id), cancellationToken);
        AddAudit(actor.User.Id, user.Id, "created", new
        {
            user.Email,
            user.DisplayName,
            Role = role,
            Permissions = permissions
        });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user.Id;
    }

    public async Task UpdateAsync(Guid userId, UpdateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await EnsurePermissionAsync(AdminUserPermissions.AdminUsersManage, cancellationToken);
        var user = await GetUserForUpdateAsync(userId, cancellationToken);
        var currentRole = GetRole(user);
        if (!IsOwner(actor) && (currentRole == OwnerRole || NormalizeRoleOrNull(request.Role) == OwnerRole))
            throw PermissionDenied();

        var newRole = NormalizeRoleOrNull(request.Role) ?? currentRole;
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? user.DisplayName
            : request.DisplayName.Trim();
        ValidateDisplayName(displayName);
        var status = NormalizeStatusOrNull(request.Status) ?? user.Status;
        var requestedPermissions = request.Permissions
            ?? (newRole == currentRole
                ? ParsePermissions(user.PermissionsJson)
                : DefaultPermissionsForRole(newRole));
        var permissions = NormalizePermissions(newRole, requestedPermissions);

        if (currentRole == OwnerRole && (newRole != OwnerRole || status != UserStatus.Active))
            await EnsureAnotherActiveOwnerAsync(user.Id, cancellationToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var roleEntity = newRole == currentRole
            ? null
            : await EnsureRoleAsync(newRole, cancellationToken);
        var beforePermissions = ParsePermissions(user.PermissionsJson);
        var beforeStatus = user.Status;
        var before = ToAuditSnapshot(user, currentRole);
        if (roleEntity is not null)
        {
            _db.UserRoles.RemoveRange(user.UserRoles);
            await _db.UserRoles.AddAsync(new UserRole(user.Id, roleEntity.Id), cancellationToken);
        }
        user.UpdateProfile(displayName, SerializePermissions(permissions), status, _clock.UtcNow);
        AddAudit(actor.User.Id, user.Id, "updated", new
        {
            Before = before,
            After = ToAuditSnapshot(user, newRole)
        });
        await _db.SaveChangesAsync(cancellationToken);
        var permissionsChanged = !permissions.SequenceEqual(beforePermissions, StringComparer.OrdinalIgnoreCase);
        if (newRole != currentRole || permissionsChanged || status != beforeStatus)
            await RevokeAdminRefreshTokensAsync(user.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid userId, ResetAdminUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await EnsurePermissionAsync(AdminUserPermissions.AdminUsersManage, cancellationToken);
        ValidatePassword(request.NewPassword);
        var user = await GetUserForUpdateAsync(userId, cancellationToken);
        var role = GetRole(user);
        if (!IsOwner(actor) && role == OwnerRole)
            throw PermissionDenied();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        user.ResetPassword(_passwordHasher.Hash(request.NewPassword), _clock.UtcNow);
        AddAudit(actor.User.Id, user.Id, "password_reset", new { user.Email, Role = role });
        await _db.SaveChangesAsync(cancellationToken);
        await RevokeAdminRefreshTokensAsync(user.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<AdminActor> EnsurePermissionAsync(string permission, CancellationToken cancellationToken)
    {
        var actor = await GetCurrentAdminAsync(cancellationToken);
        if (!HasPermission(actor, permission))
            throw PermissionDenied();

        return actor;
    }

    private async Task<AdminActor> GetCurrentAdminAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserType != "Admin"
            || _currentUser.UserId is not Guid userId)
            throw new UnauthorizedException("Admin authentication is required.");

        var user = await _db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("Admin user no longer exists.");

        if (user.Status != UserStatus.Active)
            throw new UnauthorizedException("Admin user is disabled.");

        var role = GetRole(user);
        var permissions = ParsePermissions(user.PermissionsJson);
        return new AdminActor(user, role, permissions);
    }

    private async Task<User> GetUserForUpdateAsync(Guid userId, CancellationToken cancellationToken)
        => await _db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
           ?? throw new NotFoundException("Admin user was not found.");

    private async Task<Role> EnsureRoleAsync(string role, CancellationToken cancellationToken)
    {
        var entity = await _db.Roles.FirstOrDefaultAsync(x => x.Name == role, cancellationToken);
        if (entity is not null)
            return entity;

        entity = new Role(role);
        await _db.Roles.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task EnsureAnotherActiveOwnerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var exists = await _db.Users
            .AnyAsync(x => x.Id != userId
                           && x.Status == UserStatus.Active
                           && x.UserRoles.Any(ur => ur.Role.Name == OwnerRole),
                cancellationToken);
        if (!exists)
            throw new BadRequestException("At least one active Owner is required.");
    }

    private async Task RevokeAdminRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await _db.RefreshTokens
            .Where(x => x.SubjectId == userId && x.UserType == "Admin" && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAtUtc, now)
                    .SetProperty(x => x.UpdatedAtUtc, now),
                cancellationToken);
    }

    private void AddAudit(Guid? actorUserId, Guid targetUserId, string action, object details)
    {
        _db.AdminUserAuditLogs.Add(new AdminUserAuditLog(
            actorUserId,
            targetUserId,
            action,
            JsonSerializer.Serialize(details, JsonOptions),
            _clock.UtcNow));
    }

    private static AdminUserResponse ToResponse(User user)
        => new(
            user.Id,
            user.Email,
            user.DisplayName,
            GetRole(user),
            ParsePermissions(user.PermissionsJson),
            user.Status.ToString(),
            user.CreatedAtUtc,
            user.LastLoginAtUtc);

    private static object ToAuditSnapshot(User user, string role) => new
    {
        user.Email,
        user.DisplayName,
        Role = role,
        Permissions = ParsePermissions(user.PermissionsJson),
        user.PermissionsJson,
        user.Status
    };

    private static bool HasPermission(AdminActor actor, string permission)
        => IsOwner(actor)
           || actor.Permissions.Contains(AdminUserPermissions.All, StringComparer.OrdinalIgnoreCase)
           || actor.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase)
           || (permission == AdminUserPermissions.AdminUsersRead
               && actor.Permissions.Contains(AdminUserPermissions.AdminUsersManage, StringComparer.OrdinalIgnoreCase));

    private static bool IsOwner(AdminActor actor) => actor.Role == OwnerRole;

    private static string GetRole(User user)
        => user.UserRoles
               .Select(x => x.Role.Name)
               .OrderBy(RoleSortOrder)
               .FirstOrDefault()
           ?? StaffRole;

    private static int RoleSortOrder(string role)
    {
        var index = Array.IndexOf(SupportedRoles, role);
        return index >= 0 ? index : int.MaxValue;
    }

    private static string NormalizeRole(string role)
    {
        var normalized = NormalizeRoleOrNull(role);
        return normalized ?? throw new BadRequestException("Role must be Owner, Admin, or Staff.");
    }

    private static string? NormalizeRoleOrNull(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        return SupportedRoles.FirstOrDefault(x => string.Equals(x, role.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? throw new BadRequestException("Role must be Owner, Admin, or Staff.");
    }

    private static UserStatus? NormalizeStatusOrNull(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;
        if (Enum.TryParse<UserStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new BadRequestException("Status must be Active or Disabled.");
    }

    private static IReadOnlyCollection<string> NormalizePermissions(
        string role,
        IReadOnlyCollection<string>? permissions)
    {
        if (role == OwnerRole)
            return AdminUserPermissions.OwnerPermissions;

        var values = (permissions ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (values.Any(x => x == AdminUserPermissions.All))
            throw new BadRequestException("Only Owner can have wildcard permissions.");
        var invalid = values.FirstOrDefault(x => !AdminUserPermissions.Allowed.Contains(x));
        if (invalid is not null)
            throw new BadRequestException($"Permission is not supported: {invalid}");

        return values;
    }

    private static IReadOnlyCollection<string> DefaultPermissionsForRole(string role)
        => role switch
        {
            OwnerRole => AdminUserPermissions.OwnerPermissions,
            AdminRole => AdminUserPermissions.AdminDefaultPermissions,
            _ => []
        };

    public static IReadOnlyCollection<string> ParsePermissions(string? permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<string[]>(permissionsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string SerializePermissions(IReadOnlyCollection<string> permissions)
        => JsonSerializer.Serialize(permissions, JsonOptions);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static void ValidateEmail(string email)
    {
        var normalized = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 320 || !normalized.Contains('@'))
            throw new BadRequestException("Valid email is required.");
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new BadRequestException("Password must be at least 8 characters.");
    }

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 256)
            throw new BadRequestException("Display name is required and must not exceed 256 characters.");
    }

    private static ForbiddenException PermissionDenied()
        => new("Permission denied.", "permission_denied");

    private sealed record AdminActor(User User, string Role, IReadOnlyCollection<string> Permissions);
}
