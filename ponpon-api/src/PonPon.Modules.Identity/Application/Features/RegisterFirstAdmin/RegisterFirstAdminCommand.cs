namespace PonPon.Modules.Identity.Application.Features.RegisterFirstAdmin;

public sealed record RegisterFirstAdminCommand(
    string Email,
    string Password,
    string? DisplayName);
