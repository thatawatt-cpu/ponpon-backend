namespace PonPon.Modules.Identity.Infrastructure.Seeding;

public sealed class SeedAdminOptions
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = "PonPon Admin";
}
