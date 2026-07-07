using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Application;

public sealed record PreparedPaymentCharge(
    Order Order,
    decimal AmountBaht,
    int AmountSatang,
    OmiseChargeResult? ExistingCharge);

public sealed class PaymentChargeCoordinator
{
    private const int OmiseMinimumAmount = 2000;

    private readonly IOrderRepository _orders;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IOmiseClient _omise;
    private readonly IDateTimeProvider _clock;

    public PaymentChargeCoordinator(
        IOrderRepository orders,
        IOrderingUnitOfWork unitOfWork,
        IOmiseClient omise,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _omise = omise;
        _clock = clock;
    }

    public async Task<PreparedPaymentCharge> PrepareAsync(
        Guid orderId,
        Guid customerId,
        string expectedSourceType,
        CancellationToken cancellationToken)
    {
        await using var paymentLock = await _orders.AcquirePaymentLockAsync(orderId, cancellationToken);
        var order = await _orders.GetCustomerOrderByIdAsync(orderId, customerId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        if (!string.IsNullOrWhiteSpace(order.CheckoutPaymentMethod)
            && !string.Equals(
                order.CheckoutPaymentMethod,
                expectedSourceType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                $"This order must be paid with {order.CheckoutPaymentMethod}.");
        }

        var amountBaht = OrderPaymentAmountCalculator.Calculate(order);
        var amountSatang = OrderPaymentSecurity.ToSatang(amountBaht);
        if (amountSatang < OmiseMinimumAmount)
        {
            throw new BadRequestException(
                $"ยอดชำระต้องไม่ต่ำกว่า ฿20 (ยอดที่ระบุ: ฿{amountBaht:0.##})");
        }

        var existingCharge = await OrderPaymentSecurity.GetReusableChargeAsync(
            order,
            _omise,
            amountSatang,
            expectedSourceType,
            _clock.UtcNow,
            cancellationToken);

        if (existingCharge is not null)
        {
            order.RegisterOmiseCharge(existingCharge.ChargeId, _clock.UtcNow);
            order.CompletePaymentCreation(_clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await paymentLock.CompleteAsync(cancellationToken);
            return new PreparedPaymentCharge(order, amountBaht, amountSatang, existingCharge);
        }

        if (order.IsPaymentCreationPending)
        {
            throw new BadRequestException(
                "A previous payment creation attempt is awaiting reconciliation. No new charge was created.");
        }

        order.BeginPaymentCreation(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await paymentLock.CompleteAsync(cancellationToken);
        return new PreparedPaymentCharge(order, amountBaht, amountSatang, null);
    }

    public async Task CompleteAsync(
        Guid orderId,
        Guid customerId,
        string expectedSourceType,
        int expectedAmountSatang,
        OmiseChargeResult charge,
        CancellationToken cancellationToken)
    {
        await using var paymentLock = await _orders.AcquirePaymentLockAsync(orderId, cancellationToken);
        var order = await _orders.GetCustomerOrderByIdAsync(orderId, customerId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        OrderPaymentSecurity.ValidateCharge(order, charge, expectedAmountSatang);
        OrderPaymentSecurity.ValidatePaymentMethod(charge, expectedSourceType);
        order.RegisterOmiseCharge(charge.ChargeId, _clock.UtcNow);
        order.CompletePaymentCreation(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await paymentLock.CompleteAsync(cancellationToken);
    }
}
