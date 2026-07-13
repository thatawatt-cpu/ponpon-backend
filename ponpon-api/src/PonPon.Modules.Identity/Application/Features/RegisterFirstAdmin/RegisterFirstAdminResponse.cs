namespace PonPon.Modules.Identity.Application.Features.RegisterFirstAdmin;

public sealed record RegisterFirstAdminResponse(
    Guid UserId,
    string Email,
    string DisplayName);
