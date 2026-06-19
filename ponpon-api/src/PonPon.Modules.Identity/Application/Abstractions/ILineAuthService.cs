using PonPon.Modules.Identity.Domain.Customers;

namespace PonPon.Modules.Identity.Application.Abstractions;

public interface ILineAuthService
{
    Task<LineProfile> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
