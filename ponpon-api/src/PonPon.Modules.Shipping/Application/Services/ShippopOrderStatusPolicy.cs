using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Shipping.Application.Services;

internal static class ShippopOrderStatusPolicy
{
    public static bool TryGetOrderProgress(string status, out ShippingOrderProgress progress)
    {
        switch (status.Trim().ToLowerInvariant())
        {
            case "shipping":
                progress = ShippingOrderProgress.Shipping;
                return true;
            case "complete":
                progress = ShippingOrderProgress.Completed;
                return true;
            case "invalid":
            case "problem":
                progress = ShippingOrderProgress.Failed;
                return true;
            case "return":
            case "return_shipping":
            case "return_problem":
            case "return_complete":
            case "return_return":
            case "return_close":
                progress = ShippingOrderProgress.Returned;
                return true;
            default:
                progress = default;
                return false;
        }
    }

    public static bool IsProblemStatus(string status)
    {
        return status.Trim().ToLowerInvariant() is
            "cancel" or
            "invalid" or
            "problem" or
            "return" or
            "return_shipping" or
            "return_problem" or
            "return_complete" or
            "return_return" or
            "return_close";
    }
}
