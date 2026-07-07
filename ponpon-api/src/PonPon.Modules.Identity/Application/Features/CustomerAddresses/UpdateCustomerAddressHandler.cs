using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Application.Features.CustomerAddresses;

public sealed class UpdateCustomerAddressHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateCustomerAddressHandler(
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

    public async Task<CustomerAddressResponse> HandleAsync(
        UpdateCustomerAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var address = await _addresses.GetByIdAsync(command.AddressId, customerId, cancellationToken)
            ?? throw new NotFoundException("Customer address was not found.");

        Validate(command);

        var now = _clock.UtcNow;
        address.Update(
            command.RecipientName.Trim(),
            command.Phone.Trim(),
            NormalizeOptional(command.Email),
            command.AddressLine1.Trim(),
            NormalizeOptional(command.AddressLine2),
            command.Subdistrict.Trim(),
            command.District.Trim(),
            command.Province.Trim(),
            command.Postcode.Trim(),
            string.IsNullOrWhiteSpace(command.Country) ? "TH" : command.Country.Trim().ToUpperInvariant(),
            NormalizeOptional(command.Label),
            now);

        if (command.IsDefault && !address.IsDefault)
        {
            var currentDefault = await _addresses.GetDefaultAsync(customerId, cancellationToken);
            if (currentDefault is not null)
            {
                currentDefault.SetDefault(false, now);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            address.SetDefault(true, now);
        }

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

    private static void Validate(UpdateCustomerAddressCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RecipientName)
            || string.IsNullOrWhiteSpace(command.Phone)
            || string.IsNullOrWhiteSpace(command.AddressLine1)
            || string.IsNullOrWhiteSpace(command.Subdistrict)
            || string.IsNullOrWhiteSpace(command.District)
            || string.IsNullOrWhiteSpace(command.Province)
            || string.IsNullOrWhiteSpace(command.Postcode))
        {
            throw new BadRequestException("Recipient name, phone, address, subdistrict, district, province and postcode are required.");
        }

        if (!string.IsNullOrWhiteSpace(command.Country) && command.Country.Trim().Length != 2)
        {
            throw new BadRequestException("Country must be a 2-letter country code.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
