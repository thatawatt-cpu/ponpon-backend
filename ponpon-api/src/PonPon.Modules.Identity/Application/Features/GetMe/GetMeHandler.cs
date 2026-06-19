using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.GetMe;

public sealed class GetMeHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerRepository _customers;
    private readonly IUserRepository _users;

    public GetMeHandler(ICurrentUser currentUser, ICustomerRepository customers, IUserRepository users)
    {
        _currentUser = currentUser;
        _customers = customers;
        _users = users;
    }

    public async Task<MeResponse> HandleAsync(GetMeQuery query, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        if (_currentUser.UserType == "Customer" && _currentUser.CustomerId is Guid customerId)
        {
            var customer = await _customers.GetByIdAsync(customerId, cancellationToken) ?? throw new UnauthorizedException("Customer no longer exists.");
            return new MeResponse(customer.Id, "Customer", customer.LineProfile.DisplayName, customer.LineProfile.Email, customer.LineProfile.PictureUrl, []);
        }

        if (_currentUser.UserType == "Admin" && _currentUser.UserId is Guid userId)
        {
            var user = await _users.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException("Admin user no longer exists.");
            return new MeResponse(user.Id, "Admin", user.DisplayName, user.Email, null, user.UserRoles.Select(x => x.Role.Name).ToArray());
        }

        throw new UnauthorizedException("Invalid token subject.");
    }
}
