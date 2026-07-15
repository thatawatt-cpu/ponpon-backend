using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Identity.Domain.Customers;
using PonPon.Modules.Identity.Infrastructure.Persistence;
using PonPon.Modules.Ordering.Infrastructure.Persistence;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Api.Controllers;

[ApiController]
[Authorize(Policy = "permission:customers.read")]
public sealed class AdminCustomersController : ControllerBase
{
    [HttpGet("api/admin/customers")]
    public async Task<ActionResult<AdminCustomersResponse>> GetCustomers(
        [FromQuery] AdminCustomersRequest request,
        [FromServices] IdentityDbContext identity,
        [FromServices] OrderingDbContext ordering,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.Search?.Trim();

        var customersQuery = identity.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            customersQuery = customersQuery.Where(x =>
                EF.Functions.ILike(x.LineProfile.DisplayName, $"%{search}%")
                || EF.Functions.ILike(x.LineProfile.LineUserId, $"%{search}%")
                || (x.LineProfile.Email != null && EF.Functions.ILike(x.LineProfile.Email, $"%{search}%")));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            customersQuery = ApplyStatusFilter(customersQuery, request.Status);
        }

        if (request.CreatedFromUtc.HasValue)
        {
            customersQuery = customersQuery.Where(x => x.CreatedAtUtc >= request.CreatedFromUtc.Value);
        }

        if (request.CreatedToUtc.HasValue)
        {
            customersQuery = customersQuery.Where(x => x.CreatedAtUtc <= request.CreatedToUtc.Value);
        }

        var summary = await BuildSummaryAsync(identity, ordering, cancellationToken);
        var customers = await customersQuery
            .Select(x => new CustomerProjection(
                x.Id,
                x.LineProfile.DisplayName,
                x.LineProfile.LineUserId,
                x.LineProfile.PictureUrl,
                x.LineProfile.Email,
                x.Status.ToString(),
                x.CreatedAtUtc,
                x.LastLoginAtUtc))
            .ToArrayAsync(cancellationToken);

        var customerIds = customers.Select(x => x.CustomerId).ToArray();
        var orderStats = customerIds.Length == 0
            ? new Dictionary<Guid, CustomerOrderStats>()
            : await GetOrderStatsAsync(ordering, customerIds, cancellationToken);
        var allItems = customers
            .Select(customer =>
            {
                orderStats.TryGetValue(customer.CustomerId, out var stats);
                return new AdminCustomerListItemResponse(
                    customer.CustomerId,
                    customer.DisplayName,
                    customer.LineUserId,
                    customer.PictureUrl,
                    customer.Email,
                    customer.Status,
                    stats?.OrderCount ?? 0,
                    stats?.TotalSpent ?? 0,
                    stats?.LastOrderAtUtc,
                    customer.CreatedAtUtc,
                    customer.LastLoginAtUtc);
            })
            .ToArray();

        if (request.HasOrders.HasValue)
        {
            allItems = allItems
                .Where(x => request.HasOrders.Value ? x.OrderCount > 0 : x.OrderCount == 0)
                .ToArray();
        }

        allItems = ApplySort(allItems, request.SortBy, request.SortDirection);
        var totalItems = allItems.Length;
        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return Ok(new AdminCustomersResponse(
            summary,
            items,
            page,
            pageSize,
            totalItems,
            totalItems));
    }

    private static async Task<AdminCustomersSummaryResponse> BuildSummaryAsync(
        IdentityDbContext identity,
        OrderingDbContext ordering,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var newCustomerCutoff = nowUtc.AddDays(-30);
        var totalCustomers = await identity.Customers.AsNoTracking().CountAsync(cancellationToken);
        var newCustomersLast30Days = await identity.Customers
            .AsNoTracking()
            .CountAsync(x => x.CreatedAtUtc >= newCustomerCutoff, cancellationToken);
        var paidOrderStats = await ordering.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId.HasValue && (x.PaymentStatus == "Paid" || x.PaymentStatus == "1"))
            .GroupBy(x => 1)
            .Select(g => new
            {
                OrderCount = g.Count(),
                TotalSpent = g.Sum(x => x.PaymentAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);
        var paidCustomerOrderCounts = await ordering.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId.HasValue && (x.PaymentStatus == "Paid" || x.PaymentStatus == "1"))
            .GroupBy(x => x.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, OrderCount = g.Count() })
            .ToArrayAsync(cancellationToken);

        var purchasingCustomers = paidCustomerOrderCounts.Length;
        var repeatCustomers = paidCustomerOrderCounts.Count(x => x.OrderCount > 1);
        var repeatCustomerRate = purchasingCustomers == 0
            ? 0
            : Math.Round(repeatCustomers * 100m / purchasingCustomers, 2);
        var averageOrderValue = paidOrderStats is null || paidOrderStats.OrderCount == 0
            ? 0
            : Math.Round(paidOrderStats.TotalSpent / paidOrderStats.OrderCount, 2);

        return new AdminCustomersSummaryResponse(
            totalCustomers,
            repeatCustomerRate,
            averageOrderValue,
            newCustomersLast30Days,
            purchasingCustomers);
    }

    private static async Task<Dictionary<Guid, CustomerOrderStats>> GetOrderStatsAsync(
        OrderingDbContext ordering,
        IReadOnlyCollection<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var stats = await ordering.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId.HasValue && customerIds.Contains(x.CustomerId.Value))
            .GroupBy(x => x.CustomerId!.Value)
            .Select(g => new CustomerOrderStats(
                g.Key,
                g.Count(),
                g.Where(x => x.PaymentStatus == "Paid" || x.PaymentStatus == "1").Sum(x => x.PaymentAmount),
                g.Max(x => x.OrderDate ?? x.CreatedAtUtc)))
            .ToArrayAsync(cancellationToken);

        return stats.ToDictionary(x => x.CustomerId);
    }

    private static IQueryable<Customer> ApplyStatusFilter(IQueryable<Customer> query, string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "active" => query.Where(x => x.Status == CustomerStatus.Active),
            "inactive" or "disabled" => query.Where(x => x.Status == CustomerStatus.Disabled),
            _ => throw new BadRequestException("Customer status must be Active or Inactive.")
        };
    }

    private static AdminCustomerListItemResponse[] ApplySort(
        AdminCustomerListItemResponse[] items,
        string? sortBy,
        string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy)
            ? "createdAtUtc"
            : sortBy.Trim();

        IOrderedEnumerable<AdminCustomerListItemResponse> sorted = normalizedSortBy.ToLowerInvariant() switch
        {
            "ordercount" => descending
                ? items.OrderByDescending(x => x.OrderCount)
                : items.OrderBy(x => x.OrderCount),
            "totalspent" => descending
                ? items.OrderByDescending(x => x.TotalSpent)
                : items.OrderBy(x => x.TotalSpent),
            "lastloginatutc" => descending
                ? items.OrderByDescending(x => x.LastLoginAtUtc)
                : items.OrderBy(x => x.LastLoginAtUtc),
            "createdatutc" => descending
                ? items.OrderByDescending(x => x.CreatedAtUtc)
                : items.OrderBy(x => x.CreatedAtUtc),
            _ => throw new BadRequestException(
                "Customer sortBy must be createdAtUtc, orderCount, totalSpent, or lastLoginAtUtc.")
        };

        return sorted.ToArray();
    }

    private sealed record CustomerProjection(
        Guid CustomerId,
        string DisplayName,
        string LineUserId,
        string? PictureUrl,
        string? Email,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? LastLoginAtUtc);

    private sealed record CustomerOrderStats(
        Guid CustomerId,
        int OrderCount,
        decimal TotalSpent,
        DateTime? LastOrderAtUtc);
}

public sealed record AdminCustomersRequest(
    string? Search = null,
    string? Status = null,
    bool? HasOrders = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null,
    string? SortBy = null,
    string? SortDirection = null,
    int Page = 1,
    int PageSize = 20);

public sealed record AdminCustomersResponse(
    AdminCustomersSummaryResponse Summary,
    IReadOnlyCollection<AdminCustomerListItemResponse> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalItems);

public sealed record AdminCustomersSummaryResponse(
    int TotalCustomers,
    decimal RepeatCustomerRate,
    decimal AverageOrderValue,
    int NewCustomersLast30Days,
    int CustomersWithOrders);

public sealed record AdminCustomerListItemResponse(
    Guid CustomerId,
    string DisplayName,
    string LineUserId,
    string? PictureUrl,
    string? Email,
    string Status,
    int OrderCount,
    decimal TotalSpent,
    DateTime? LastOrderAtUtc,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);
