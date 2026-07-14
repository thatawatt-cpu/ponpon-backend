namespace PonPon.Modules.Identity.Application.AdminUsers;

public static class AdminUserPermissions
{
    public const string All = "*";
    public const string AdminUsersRead = "admin_users.read";
    public const string AdminUsersManage = "admin_users.manage";

    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "dashboard.read",
        "orders.read",
        "orders.manage",
        "orders.refund",
        "products.read",
        "products.manage",
        "customers.read",
        "reviews.manage",
        "marketing.manage",
        "integrations.read",
        "integrations.manage",
        "settings.manage",
        AdminUsersRead,
        AdminUsersManage
    };

    public static readonly IReadOnlyCollection<string> OwnerPermissions = [All];

    public static readonly IReadOnlyCollection<string> AdminDefaultPermissions =
    [
        "dashboard.read",
        "orders.read",
        "orders.manage",
        "orders.refund",
        "products.read",
        "products.manage",
        "customers.read",
        "reviews.manage",
        "marketing.manage",
        "integrations.read",
        AdminUsersRead
    ];
}
