namespace PonPon.Modules.Identity.Application.Features.RegisterFirstAdmin;

public sealed record RegisterFirstAdminRequest(
    string Email,
    string Password,
    string? DisplayName);
