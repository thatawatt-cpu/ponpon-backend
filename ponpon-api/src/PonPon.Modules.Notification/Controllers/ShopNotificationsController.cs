using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Notification.Domain;
using PonPon.Modules.Notification.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Notification.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class ShopNotificationsController : ControllerBase
{
    private readonly NotificationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ShopNotificationsController(
        NotificationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    [HttpGet]
    public async Task<ActionResult<ShopNotificationsResponse>> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var target = GetCustomerTarget();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = BuildTargetQuery(target);
        var unreadCount = await query.CountAsync(x => x.ReadAtUtc == null, cancellationToken);

        if (unreadOnly)
        {
            query = query.Where(x => x.ReadAtUtc == null);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ShopNotificationResponse(
                x.Id,
                x.Type,
                x.OrderId,
                x.OrderNumber,
                x.Title,
                x.Message,
                x.Amount,
                x.Status,
                x.TrackingNumber,
                x.ActionUrl,
                x.ReadAtUtc != null,
                x.ReadAtUtc,
                x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return Ok(new ShopNotificationsResponse(items, unreadCount));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ShopNotificationUnreadCountResponse>> GetUnreadCount(
        CancellationToken cancellationToken = default)
    {
        var target = GetCustomerTarget();
        var count = await BuildTargetQuery(target)
            .CountAsync(x => x.ReadAtUtc == null, cancellationToken);

        return Ok(new ShopNotificationUnreadCountResponse(count));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var target = GetCustomerTarget();
        var notification = await BuildTargetQuery(target)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Notification was not found.");

        notification.MarkRead(_clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        var target = GetCustomerTarget();
        var now = _clock.UtcNow;
        var notifications = await BuildTargetQuery(target)
            .Where(x => x.ReadAtUtc == null)
            .ToArrayAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkRead(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<ShopNotification> BuildTargetQuery(CustomerNotificationTarget target)
    {
        var query = _dbContext.ShopNotifications.AsQueryable();

        if (target.CustomerId is Guid customerId && !string.IsNullOrWhiteSpace(target.LineUserId))
        {
            return query.Where(x => x.CustomerId == customerId || x.LineUserId == target.LineUserId);
        }

        if (target.CustomerId is Guid id)
        {
            return query.Where(x => x.CustomerId == id);
        }

        return query.Where(x => x.LineUserId == target.LineUserId);
    }

    private CustomerNotificationTarget GetCustomerTarget()
    {
        if (!_currentUser.IsAuthenticated
            || !string.Equals(_currentUser.UserType, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException("Customer authentication is required.");
        }

        if (_currentUser.CustomerId is null && string.IsNullOrWhiteSpace(_currentUser.LineUserId))
        {
            throw new UnauthorizedException("Customer notification target is missing.");
        }

        return new CustomerNotificationTarget(_currentUser.CustomerId, _currentUser.LineUserId);
    }

    private sealed record CustomerNotificationTarget(Guid? CustomerId, string? LineUserId);
}

public sealed record ShopNotificationsResponse(
    IReadOnlyCollection<ShopNotificationResponse> Items,
    int UnreadCount);

public sealed record ShopNotificationUnreadCountResponse(int UnreadCount);

public sealed record ShopNotificationResponse(
    Guid Id,
    string Type,
    Guid OrderId,
    string OrderNumber,
    string Title,
    string Message,
    decimal? Amount,
    string? Status,
    string? TrackingNumber,
    string? ActionUrl,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc);
