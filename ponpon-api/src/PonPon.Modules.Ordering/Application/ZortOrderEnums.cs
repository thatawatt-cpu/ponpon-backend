namespace PonPon.Modules.Ordering.Application;

public enum ZortOrderStatus
{
    Pending = 0,
    Success = 1,
    Voided = 2,
    Waiting = 3,
    Returned = 4,
    Packed = 5,
    Shipping = 6,
    FailedShipment = 7
}

public enum ZortPaymentStatus
{
    Pending = 0,
    Paid = 1,
    Voided = 2,
    PartialPayment = 3,
    ExcessPayment = 4
}
