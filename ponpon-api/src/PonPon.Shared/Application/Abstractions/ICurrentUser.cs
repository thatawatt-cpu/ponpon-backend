namespace PonPon.Shared.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? CustomerId { get; }
    string? UserType { get; }
    string? LineUserId { get; }
    IReadOnlyCollection<string> Roles { get; }
}
