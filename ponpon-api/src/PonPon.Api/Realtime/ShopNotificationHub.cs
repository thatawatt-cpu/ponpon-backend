using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PonPon.Api.Realtime;

[Authorize]
public sealed class ShopNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userType = Context.User?.FindFirstValue("userType");
        if (!string.Equals(userType, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            Context.Abort();
            return;
        }

        var customerId = Context.User?.FindFirstValue("customerId");
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ShopNotificationGroups.Customer(customerId),
                Context.ConnectionAborted);
        }

        var lineUserId = Context.User?.FindFirstValue("lineUserId");
        if (!string.IsNullOrWhiteSpace(lineUserId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ShopNotificationGroups.LineUser(lineUserId),
                Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }
}

internal static class ShopNotificationGroups
{
    public static string Customer(Guid customerId) => Customer(customerId.ToString("D"));
    public static string Customer(string customerId) => $"shop:customer:{customerId.Trim()}";
    public static string LineUser(string lineUserId) => $"shop:line:{lineUserId.Trim()}";
}
