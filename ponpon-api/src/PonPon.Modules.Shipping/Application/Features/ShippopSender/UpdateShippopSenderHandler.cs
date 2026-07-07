using System.Net.Mail;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;
using SenderEntity = PonPon.Modules.Shipping.Domain.ShippopSender;

namespace PonPon.Modules.Shipping.Application.Features.ShippopSender;

public sealed class UpdateShippopSenderHandler
{
    private readonly IShippopSenderRepository _senders;
    private readonly IShippingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateShippopSenderHandler(
        IShippopSenderRepository senders,
        IShippingUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _senders = senders;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ShippopSenderResponse> HandleAsync(
        UpdateShippopSenderCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        var now = _clock.UtcNow;
        var values = Normalize(command);
        var sender = await _senders.GetAsync(cancellationToken);

        if (sender is null)
        {
            sender = SenderEntity.Create(
                values.Name,
                values.Phone,
                values.Email,
                values.Address,
                values.District,
                values.State,
                values.Province,
                values.Postcode,
                now);
            await _senders.AddAsync(sender, cancellationToken);
        }
        else
        {
            sender.Update(
                values.Name,
                values.Phone,
                values.Email,
                values.Address,
                values.District,
                values.State,
                values.Province,
                values.Postcode,
                now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ShippopSenderResponse.From(sender);
    }

    private static void Validate(UpdateShippopSenderCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)
            || string.IsNullOrWhiteSpace(command.Phone)
            || string.IsNullOrWhiteSpace(command.Email)
            || string.IsNullOrWhiteSpace(command.Address)
            || string.IsNullOrWhiteSpace(command.District)
            || string.IsNullOrWhiteSpace(command.State)
            || string.IsNullOrWhiteSpace(command.Province)
            || string.IsNullOrWhiteSpace(command.Postcode))
        {
            throw new BadRequestException(
                "Name, phone, email, address, district, state, province and postcode are required.");
        }

        if (!MailAddress.TryCreate(command.Email.Trim(), out _))
            throw new BadRequestException("Sender email is invalid.");

        var postcode = command.Postcode.Trim();
        if (postcode.Length != 5 || !postcode.All(char.IsDigit))
            throw new BadRequestException("Sender postcode must contain exactly 5 digits.");
    }

    private static UpdateShippopSenderCommand Normalize(UpdateShippopSenderCommand command) =>
        new(
            command.Name.Trim(),
            command.Phone.Trim(),
            command.Email.Trim(),
            command.Address.Trim(),
            command.District.Trim(),
            command.State.Trim(),
            command.Province.Trim(),
            command.Postcode.Trim());
}
