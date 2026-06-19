using System.Security.Claims;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Api.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public Guid? UserId => ReadGuid("userId");
    public Guid? CustomerId => ReadGuid("customerId");
    public string? UserType => User?.FindFirstValue("userType");
    public string? LineUserId => User?.FindFirstValue("lineUserId");
    public IReadOnlyCollection<string> Roles => User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray() ?? [];

    private Guid? ReadGuid(string claimType)
    {
        var value = User?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
