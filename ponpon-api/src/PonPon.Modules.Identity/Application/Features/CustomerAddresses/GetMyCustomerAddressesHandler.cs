using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.CustomerAddresses;

public sealed class GetMyCustomerAddressesHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerAddressRepository _addresses;

    public GetMyCustomerAddressesHandler(ICurrentUser currentUser, ICustomerAddressRepository addresses)
    {
        _currentUser = currentUser;
        _addresses = addresses;
    }

    public async Task<IReadOnlyCollection<CustomerAddressResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var addresses = await _addresses.GetByCustomerIdAsync(customerId, cancellationToken);
        return addresses.Select(x => x.ToResponse()).ToArray();
    }

    private Guid GetCustomerId()
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserType != "Customer"
            || _currentUser.CustomerId is not Guid customerId)
        {
            throw new UnauthorizedException("Customer authentication is required.");
        }

        return customerId;
    }
}
