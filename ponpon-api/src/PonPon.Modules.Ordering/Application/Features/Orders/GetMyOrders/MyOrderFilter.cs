namespace PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;

public enum MyOrderFilter
{
    PendingPayment,
    Preparing,
    AwaitingReceive,
    Completed,
    Cancelled,
    ReturnRefund,
    AwaitingReview
}
