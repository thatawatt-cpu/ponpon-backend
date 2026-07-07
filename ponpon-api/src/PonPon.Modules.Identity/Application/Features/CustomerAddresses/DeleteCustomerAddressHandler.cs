using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.Customers;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.CustomerAddresses;

public sealed class DeleteCustomerAddressHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public DeleteCustomerAddressHandler(
        ICurrentUser currentUser,
        ICustomerAddressRepository addresses,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _addresses = addresses;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var address = await _addresses.GetByIdAsync(addressId, customerId, cancellationToken)
            ?? throw new NotFoundException("Customer address was not found.");

        var wasDefault = address.IsDefault;
        var remainingAddresses = wasDefault
            ? (await _addresses.GetByCustomerIdAsync(customerId, cancellationToken))
                .Where(x => x.Id != address.Id)
                .ToArray()
            : Array.Empty<CustomerAddress>();
        var now = _clock.UtcNow;
        address.Delete(now);

        if (wasDefault)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            remainingAddresses.FirstOrDefault()?.SetDefault(true, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
