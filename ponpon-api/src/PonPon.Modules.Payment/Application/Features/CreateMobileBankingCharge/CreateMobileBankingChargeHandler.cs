using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Application.Features.CreateMobileBankingCharge;

public sealed class CreateMobileBankingChargeHandler
{
    private const string Currency = "THB";

    private static readonly HashSet<string> ValidBankTypes =
    [
        "mobile_banking_bbl",
        "mobile_banking_kbank",
        "mobile_banking_scb",
        "mobile_banking_ktb",
        "mobile_banking_bay",
    ];

    private readonly IOmiseClient _omise;
    private readonly PaymentChargeCoordinator _coordinator;
    private readonly ICurrentUser _currentUser;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;

    public CreateMobileBankingChargeHandler(
        IOmiseClient omise,
        PaymentChargeCoordinator coordinator,
        ICurrentUser currentUser,
        IShopRealtimeNotificationService shopRealtimeNotifications)
    {
        _omise = omise;
        _coordinator = coordinator;
        _currentUser = currentUser;
        _shopRealtimeNotifications = shopRealtimeNotifications;
    }

    public async Task<CreateMobileBankingChargeResponse> HandleAsync(
        CreateMobileBankingChargeCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CustomerId is not Guid customerId)
            throw new UnauthorizedException("Customer authentication is required.");

        if (!ValidBankTypes.Contains(command.BankType))
        {
            throw new BadRequestException(
                $"Unsupported bank type: {command.BankType}. Valid types: {string.Join(", ", ValidBankTypes)}");
        }

        var prepared = await _coordinator.PrepareAsync(
            command.OrderId,
            customerId,
            command.BankType,
            cancellationToken);
        var result = prepared.ExistingCharge
            ?? await _omise.CreateMobileBankingChargeAsync(
                command.BankType,
                prepared.AmountSatang,
                Currency,
                prepared.Order.Number,
                command.ReturnUri,
                cancellationToken);

        if (result.AuthorizeUri is null)
            throw new InvalidOperationException("Omise did not return an authorize URI for mobile banking.");

        if (prepared.ExistingCharge is null)
        {
            await _coordinator.CompleteAsync(
                prepared.Order.Id,
                customerId,
                command.BankType,
                prepared.AmountSatang,
                result,
                cancellationToken);
        }

        await _shopRealtimeNotifications.NotifyAsync(
            new ShopRealtimeNotification(
                prepared.Order.CustomerId,
                prepared.Order.IntegrationCustomerId,
                "payment_created",
                prepared.Order.Id,
                prepared.Order.Number,
                "พร้อมชำระเงิน",
                "ระบบสร้างรายการชำระเงินแล้ว",
                prepared.AmountBaht,
                result.Status),
            cancellationToken);

        return new CreateMobileBankingChargeResponse(
            result.ChargeId,
            result.Status,
            result.AuthorizeUri);
    }
}
