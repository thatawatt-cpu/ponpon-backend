using Microsoft.AspNetCore.Authorization;

namespace PonPon.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var isAdminUser = context.User.HasClaim(
            claim => claim.Type == "userType"
                     && string.Equals(claim.Value, "Admin", StringComparison.OrdinalIgnoreCase));
        if (!isAdminUser)
        {
            return Task.CompletedTask;
        }

        var permissions = context.User.FindAll("permission")
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (permissions.Contains("*")
            || permissions.Contains(requirement.Permission)
            || HasManagePermissionForRead(permissions, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasManagePermissionForRead(IReadOnlySet<string> permissions, string permission)
    {
        const string readSuffix = ".read";
        if (!permission.EndsWith(readSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var managePermission = permission[..^readSuffix.Length] + ".manage";
        return permissions.Contains(managePermission);
    }
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
