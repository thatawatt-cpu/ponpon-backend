using PonPon.Modules.Identity.Domain.Users;

namespace PonPon.Modules.Identity.Application.AdminUsers;

public sealed record AdminUserListResponse(
    IReadOnlyCollection<AdminUserResponse> Items,
    int Page,
    int PageSize,
    int TotalItems);

public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string> Permissions,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);

public sealed record CreateAdminUserRequest(
    string Email,
    string Password,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string>? Permissions);

public sealed record UpdateAdminUserRequest(
    string? DisplayName,
    string? Role,
    IReadOnlyCollection<string>? Permissions,
    string? Status);

public sealed record ResetAdminUserPasswordRequest(string NewPassword);

public sealed record AdminAuthMeResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    IReadOnlyCollection<string> Permissions);
