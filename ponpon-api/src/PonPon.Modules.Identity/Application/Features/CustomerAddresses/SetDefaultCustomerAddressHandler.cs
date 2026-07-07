using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.CustomerAddresses;

public sealed class SetDefaultCustomerAddressHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public SetDefaultCustomerAddressHandler(
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

    public async Task<CustomerAddressResponse> HandleAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var address = await _addresses.GetByIdAsync(addressId, customerId, cancellationToken)
            ?? throw new NotFoundException("Customer address was not found.");

        var now = _clock.UtcNow;
        var currentDefault = await _addresses.GetDefaultAsync(customerId, cancellationToken);
        if (currentDefault is not null && currentDefault.Id != address.Id)
        {
            currentDefault.SetDefault(false, now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        address.SetDefault(true, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return address.ToResponse();
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
