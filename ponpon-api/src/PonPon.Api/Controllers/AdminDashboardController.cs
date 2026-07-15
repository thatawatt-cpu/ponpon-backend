using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Domain.SyncRuns;
using PonPon.Modules.Catalog.Infrastructure.Persistence;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.Returns;
using PonPon.Modules.Ordering.Domain.SyncRuns;
using PonPon.Modules.Ordering.Infrastructure.Persistence;

namespace PonPon.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "permission:dashboard.read")]
public sealed class AdminDashboardController : ControllerBase
{
    private const string DefaultTimeZone = "Asia/Bangkok";

    private readonly OrderingDbContext _orderingDbContext;
    private readonly CatalogDbContext _catalogDbContext;

    public AdminDashboardController(
        OrderingDbContext orderingDbContext,
        CatalogDbContext catalogDbContext)
    {
        _orderingDbContext = orderingDbContext;
        _catalogDbContext = catalogDbContext;
    }

    [HttpGet]
    public async Task<ActionResult<AdminDashboardResponse>> GetDashboard(
        [FromQuery] DateOnly? date,
        [FromQuery] string period = "day",
        [FromQuery] string timeZone = DefaultTimeZone,
        [FromQuery] int lowStockThreshold = 5,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetTimeZone(timeZone, out var zone))
        {
            return BadRequest(new { message = $"Unknown time zone '{timeZone}'." });
        }
    
        lowStockThreshold = Math.Clamp(lowStockThreshold, 1, 1000);
        var localDate = date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
        if (!TryGetDashboardPeriod(period, out var dashboardPeriod))
        {
            return BadRequest(new { message = $"Unknown period '{period}'. Use day, month, or year." });
        }

        var (startDate, endDateExclusive) = GetLocalDateRange(localDate, dashboardPeriod);
        var (startUtc, endUtc) = GetUtcRange(startDate, endDateExclusive, zone);
        var (previousStartDate, previousEndDateExclusive) = GetPreviousLocalDateRange(startDate, dashboardPeriod);
        var (previousStartUtc, previousEndUtc) = GetUtcRange(previousStartDate, previousEndDateExclusive, zone);

        var todayOrders = _orderingDbContext.Orders
            .AsNoTracking()
            .Where(x =>
                (x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc) >= startUtc
                && (x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc) < endUtc);

        var allOrders = await todayOrders
            .Select(x => new { x.Amount, x.Status, x.PaymentStatus })
            .ToArrayAsync(cancellationToken);

        var nonCancelledOrders = allOrders
            .Where(x => !IsOrderStatus(x.Status, "Voided") && !IsOrderStatus(x.Status, "Returned"))
            .ToArray();
        var salesOrders = nonCancelledOrders
            .Where(x => IsPaidPaymentStatus(x.PaymentStatus))
            .ToArray();
        var salesTotal = salesOrders.Sum(x => x.Amount);
        var salesOrderCount = salesOrders.Length;
        var averagePerOrder = salesOrderCount == 0 ? 0 : decimal.Round(salesTotal / salesOrderCount, 2);

        var previousOrders = await _orderingDbContext.Orders
            .AsNoTracking()
            .Where(x =>
                (x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc) >= previousStartUtc
                && (x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc) < previousEndUtc)
            .Select(x => new { x.Amount, x.Status, x.PaymentStatus })
            .ToArrayAsync(cancellationToken);

        var previousSalesOrders = previousOrders
            .Where(x =>
                !IsOrderStatus(x.Status, "Voided")
                && !IsOrderStatus(x.Status, "Returned")
                && IsPaidPaymentStatus(x.PaymentStatus))
            .ToArray();
        var previousSalesTotal = previousSalesOrders.Sum(x => x.Amount);
        var previousSalesOrderCount = previousSalesOrders.Length;
        var previousAveragePerOrder = previousSalesOrderCount == 0
            ? 0
            : decimal.Round(previousSalesTotal / previousSalesOrderCount, 2);
        var salesChangeAmount = salesTotal - previousSalesTotal;
        var salesChangePercent = previousSalesTotal == 0
            ? (decimal?)null
            : decimal.Round(salesChangeAmount / previousSalesTotal * 100, 2);

        var paymentCounts = allOrders
            .GroupBy(x => x.PaymentStatus)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

        var pendingOrders = nonCancelledOrders.Count(x => IsOrderStatus(x.Status, "Pending"));
        var awaitingPacking = nonCancelledOrders.Count(x =>
            IsOrderStatus(x.Status, "Waiting") && IsPaidPaymentStatus(x.PaymentStatus));
        var returnRequests = await _orderingDbContext.OrderReturnRequests
            .AsNoTracking()
            .CountAsync(x => x.Status == OrderReturnRequestStatus.Requested, cancellationToken);
        var refundRequests = await _orderingDbContext.Orders
            .AsNoTracking()
            .CountAsync(
                x => x.OmiseRefundStatus == OrderRefundStatus.ManualRefundPending,
                cancellationToken);

        var latestOrders = await _orderingDbContext.Orders
            .AsNoTracking()
            .OrderByDescending(x => x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc)
            .Take(5)
            .Select(x => new DashboardOrderResponse(
                x.Id,
                x.Number,
                x.CustomerName,
                x.PaymentStatus,
                x.Amount,
                x.Status,
                x.OrderDate))
            .ToArrayAsync(cancellationToken);

        var activeProductVariants =
            from product in _catalogDbContext.Products.AsNoTracking()
            join variant in _catalogDbContext.ProductVariants.AsNoTracking()
                on product.Id equals variant.ProductId
            where product.Status == ProductStatus.Active
                  && product.IsActiveFromZort
                  && variant.Status == ProductStatus.Active
                  && variant.IsActiveFromZort
            select new { Product = product, Variant = variant };

        var lowStockCount = await activeProductVariants.CountAsync(
            x => x.Variant.AvailableStock > 0 && x.Variant.AvailableStock <= lowStockThreshold,
            cancellationToken);
        var outOfStockCount = await activeProductVariants.CountAsync(
            x => x.Variant.AvailableStock <= 0,
            cancellationToken);

        var stockAttentionVariants = await activeProductVariants
            .Where(x => x.Variant.AvailableStock <= lowStockThreshold)
            .OrderBy(x => x.Variant.AvailableStock)
            .ThenBy(x => x.Product.Name)
            .ThenBy(x => x.Variant.Sku)
            .Select(x => new
            {
                ProductId = x.Product.Id,
                VariantId = x.Variant.Id,
                x.Product.Name,
                x.Variant.Sku,
                x.Variant.VariantCode,
                x.Variant.OptionsJson,
                VariantImageUrl = x.Variant.ImageUrl,
                ProductImageUrl = x.Product.ImageUrl,
                x.Variant.AvailableStock,
                x.Product.Status
            })
            .Take(10)
            .ToArrayAsync(cancellationToken);

        var syncIssueProductRows = await _catalogDbContext.Products
            .AsNoTracking()
            .Where(x => x.Status == ProductStatus.MissingFromSource)
            .OrderBy(x => x.Name)
            .Select(product => new
            {
                ProductId = product.Id,
                product.Name,
                Sku = product.BaseSku,
                ProductImageUrl = product.ImageUrl,
                product.AvailableStock,
                product.Status
            })
            .Take(10)
            .ToArrayAsync(cancellationToken);

        var productImageFallbacks = await GetProductImageFallbacksAsync(
            stockAttentionVariants.Select(x => x.ProductId).Concat(syncIssueProductRows.Select(x => x.ProductId)),
            cancellationToken);

        var stockAttentionProducts = stockAttentionVariants
            .Select(x => new DashboardProductResponse(
                x.ProductId,
                x.VariantId,
                x.Name,
                x.Sku,
                x.VariantCode,
                ParseVariantOptions(x.OptionsJson),
                FirstNonEmpty(x.VariantImageUrl, x.ProductImageUrl, productImageFallbacks.GetValueOrDefault(x.ProductId)),
                x.AvailableStock,
                x.Status,
                x.AvailableStock <= 0 ? "OutOfStock" : "LowStock"))
            .ToArray();

        var syncIssueProducts = syncIssueProductRows
            .Select(x => new DashboardProductResponse(
                x.ProductId,
                null,
                x.Name,
                x.Sku,
                null,
                Array.Empty<DashboardProductVariantOptionResponse>(),
                FirstNonEmpty(x.ProductImageUrl, productImageFallbacks.GetValueOrDefault(x.ProductId)),
                x.AvailableStock,
                x.Status,
                "SyncIssue"))
            .ToArray();

        var attentionProducts = stockAttentionProducts
            .Concat(syncIssueProducts)
            .Take(10)
            .ToArray();

        var productSyncRuns = await _catalogDbContext.ProductSyncRuns
            .AsNoTracking()
            .Where(x => x.RequestedAtUtc >= startUtc && x.RequestedAtUtc < endUtc)
            .Select(x => new { x.Status, x.CompletedAtUtc })
            .ToArrayAsync(cancellationToken);

        var orderSyncRuns = await _orderingDbContext.OrderSyncRuns
            .AsNoTracking()
            .Where(x => x.RequestedAtUtc >= startUtc && x.RequestedAtUtc < endUtc)
            .Select(x => new { x.Status, x.CompletedAtUtc })
            .ToArrayAsync(cancellationToken);

        var syncStatuses = productSyncRuns.Select(x => (x.Status, x.CompletedAtUtc))
            .Concat(orderSyncRuns.Select(x => (x.Status, x.CompletedAtUtc)))
            .ToArray();

        var lastSuccessfulAt = syncStatuses
            .Where(x => x.Status == ProductSyncRunStatus.Succeeded)
            .Select(x => x.CompletedAtUtc)
            .Max();

        return Ok(new AdminDashboardResponse(
            localDate,
            dashboardPeriod,
            startDate,
            endDateExclusive.AddDays(-1),
            timeZone,
            new DashboardSalesResponse(
                salesTotal,
                salesOrderCount,
                averagePerOrder,
                previousSalesTotal,
                previousSalesOrderCount,
                previousAveragePerOrder,
                salesChangeAmount,
                salesChangePercent,
                GetComparisonLabel(dashboardPeriod)),
            new DashboardOrdersResponse(
                pendingOrders,
                awaitingPacking,
                returnRequests,
                refundRequests,
                latestOrders),
            new DashboardInventoryResponse(
                lowStockCount,
                outOfStockCount,
                lowStockThreshold,
                attentionProducts),
            new DashboardPaymentsResponse(
                allOrders.Length,
                allOrders.Count(x => IsPaidPaymentStatus(x.PaymentStatus)),
                GetCount(paymentCounts, "Pending"),
                GetCount(paymentCounts, "PartialPayment"),
                GetCount(paymentCounts, "ExcessPayment"),
                GetCount(paymentCounts, "Voided")),
            new DashboardSyncResponse(
                syncStatuses.Count(x => x.Status == ProductSyncRunStatus.Succeeded),
                syncStatuses.Count(x => x.Status is ProductSyncRunStatus.Pending or ProductSyncRunStatus.Running),
                syncStatuses.Count(x => x.Status is ProductSyncRunStatus.Failed or ProductSyncRunStatus.CompletedWithErrors),
                lastSuccessfulAt)));
    }

    [HttpGet("sync-runs")]
    public async Task<ActionResult<IReadOnlyCollection<DashboardSyncRunResponse>>> GetSyncRuns(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var productRuns = await _catalogDbContext.ProductSyncRuns
            .AsNoTracking()
            .OrderByDescending(x => x.RequestedAtUtc)
            .Take(limit)
            .Select(x => new SyncRunProjection(
                x.Id,
                "Products",
                x.BackgroundJobId,
                x.Status,
                x.TotalFetched,
                x.Created,
                x.Updated,
                x.Unchanged,
                x.Deactivated,
                x.Failed,
                x.ErrorsJson,
                x.RequestedAtUtc,
                x.StartedAtUtc,
                x.CompletedAtUtc))
            .ToArrayAsync(cancellationToken);

        var orderRuns = await _orderingDbContext.OrderSyncRuns
            .AsNoTracking()
            .OrderByDescending(x => x.RequestedAtUtc)
            .Take(limit)
            .Select(x => new SyncRunProjection(
                x.Id,
                "Orders",
                x.BackgroundJobId,
                x.Status,
                x.TotalFetched,
                x.Created,
                x.Updated,
                0,
                0,
                x.Failed,
                x.ErrorsJson,
                x.RequestedAtUtc,
                x.StartedAtUtc,
                x.CompletedAtUtc))
            .ToArrayAsync(cancellationToken);

        var response = productRuns
            .Concat(orderRuns)
            .OrderByDescending(x => x.RequestedAtUtc)
            .Take(limit)
            .Select(x => new DashboardSyncRunResponse(
                x.Id,
                x.Type,
                x.BackgroundJobId,
                x.Status,
                x.TotalFetched,
                x.Created,
                x.Updated,
                x.Unchanged,
                x.Deactivated,
                x.Failed,
                ParseErrors(x.ErrorsJson),
                x.RequestedAtUtc,
                x.StartedAtUtc,
                x.CompletedAtUtc))
            .ToArray();

        return Ok(response);
    }

    [HttpGet("sync-runs/{id:guid}")]
    public async Task<ActionResult<DashboardSyncRunResponse>> GetSyncRun(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var productRun = await _catalogDbContext.ProductSyncRuns
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SyncRunProjection(
                x.Id,
                "Products",
                x.BackgroundJobId,
                x.Status,
                x.TotalFetched,
                x.Created,
                x.Updated,
                0,
                0,
                x.Failed,
                x.ErrorsJson,
                x.RequestedAtUtc,
                x.StartedAtUtc,
                x.CompletedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (productRun is not null)
        {
            return Ok(ToResponse(productRun));
        }

        var orderRun = await _orderingDbContext.OrderSyncRuns
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SyncRunProjection(
                x.Id,
                "Orders",
                x.BackgroundJobId,
                x.Status,
                x.TotalFetched,
                x.Created,
                x.Updated,
                0,
                0,
                x.Failed,
                x.ErrorsJson,
                x.RequestedAtUtc,
                x.StartedAtUtc,
                x.CompletedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return orderRun is null ? NotFound() : Ok(ToResponse(orderRun));
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string status)
        => counts.GetValueOrDefault(status);

    private static bool IsOrderStatus(string status, string expected)
        => string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsPaymentStatus(string status, string expected)
        => string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsPaidPaymentStatus(string status)
        => IsPaymentStatus(status, "Paid")
           || IsPaymentStatus(status, "\u0E08\u0E48\u0E32\u0E22\u0E40\u0E07\u0E34\u0E19\u0E41\u0E25\u0E49\u0E27");

    private static bool TryGetDashboardPeriod(string period, out string dashboardPeriod)
    {
        dashboardPeriod = period.Trim().ToLowerInvariant();
        dashboardPeriod = dashboardPeriod switch
        {
            "today" => "day",
            "daily" => "day",
            "day" => "day",
            "thismonth" => "month",
            "monthly" => "month",
            "month" => "month",
            "thisyear" => "year",
            "yearly" => "year",
            "year" => "year",
            _ => string.Empty
        };

        return dashboardPeriod.Length > 0;
    }

    private static (DateOnly StartDate, DateOnly EndDateExclusive) GetLocalDateRange(
        DateOnly date,
        string period)
    {
        return period switch
        {
            "month" => (
                new DateOnly(date.Year, date.Month, 1),
                new DateOnly(date.Year, date.Month, 1).AddMonths(1)),
            "year" => (
                new DateOnly(date.Year, 1, 1),
                new DateOnly(date.Year + 1, 1, 1)),
            _ => (date, date.AddDays(1))
        };
    }

    private static (DateOnly StartDate, DateOnly EndDateExclusive) GetPreviousLocalDateRange(
        DateOnly currentStartDate,
        string period)
    {
        return period switch
        {
            "month" => (currentStartDate.AddMonths(-1), currentStartDate),
            "year" => (currentStartDate.AddYears(-1), currentStartDate),
            _ => (currentStartDate.AddDays(-1), currentStartDate)
        };
    }

    private static string GetComparisonLabel(string period)
    {
        return period switch
        {
            "month" => "\u0E40\u0E14\u0E37\u0E2D\u0E19\u0E01\u0E48\u0E2D\u0E19",
            "year" => "\u0E1B\u0E35\u0E01\u0E48\u0E2D\u0E19",
            _ => "\u0E40\u0E21\u0E37\u0E48\u0E2D\u0E27\u0E32\u0E19"
        };
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetProductImageFallbacksAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var images = await _catalogDbContext.ProductImages
            .AsNoTracking()
            .Where(x => ids.Contains(x.ProductId))
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => new { x.ProductId, x.Url })
            .ToArrayAsync(cancellationToken);

        return images
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First().Url);
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static DashboardSyncRunResponse ToResponse(SyncRunProjection syncRun)
        => new(
            syncRun.Id,
            syncRun.Type,
            syncRun.BackgroundJobId,
            syncRun.Status,
            syncRun.TotalFetched,
            syncRun.Created,
            syncRun.Updated,
            syncRun.Unchanged,
            syncRun.Deactivated,
            syncRun.Failed,
            ParseErrors(syncRun.ErrorsJson),
            syncRun.RequestedAtUtc,
            syncRun.StartedAtUtc,
            syncRun.CompletedAtUtc);

    private static IReadOnlyCollection<string> ParseErrors(string? errorsJson)
    {
        if (string.IsNullOrWhiteSpace(errorsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(errorsJson) ?? [];
        }
        catch (JsonException)
        {
            return [errorsJson];
        }
    }

    private static IReadOnlyCollection<DashboardProductVariantOptionResponse> ParseVariantOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<DashboardProductVariantOptionResponse[]>(optionsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool TryGetTimeZone(string timeZone, out TimeZoneInfo zone)
    {
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            zone = null!;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            zone = null!;
            return false;
        }
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetUtcRange(
        DateOnly startDate,
        DateOnly endDateExclusive,
        TimeZoneInfo timeZone)
    {
        var startLocal = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(endDateExclusive.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return (
            TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone));
    }

    private sealed record SyncRunProjection(
        Guid Id,
        string Type,
        string? BackgroundJobId,
        string Status,
        int TotalFetched,
        int Created,
        int Updated,
        int Unchanged,
        int Deactivated,
        int Failed,
        string ErrorsJson,
        DateTime RequestedAtUtc,
        DateTime? StartedAtUtc,
        DateTime? CompletedAtUtc);
}

public sealed record AdminDashboardResponse(
    DateOnly Date,
    string Period,
    DateOnly StartDate,
    DateOnly EndDate,
    string TimeZone,
    DashboardSalesResponse Sales,
    DashboardOrdersResponse Orders,
    DashboardInventoryResponse Inventory,
    DashboardPaymentsResponse Payments,
    DashboardSyncResponse ZortSync);

public sealed record DashboardSalesResponse(
    decimal Total,
    int OrderCount,
    decimal AveragePerOrder,
    decimal PreviousTotal,
    int PreviousOrderCount,
    decimal PreviousAveragePerOrder,
    decimal ChangeAmount,
    decimal? ChangePercent,
    string ComparisonLabel);

public sealed record DashboardOrdersResponse(
    int Pending,
    int AwaitingPacking,
    int ReturnRequests,
    int RefundRequests,
    IReadOnlyCollection<DashboardOrderResponse> Latest);

public sealed record DashboardOrderResponse(
    Guid Id,
    string Number,
    string? CustomerName,
    string PaymentStatus,
    decimal Amount,
    string Status,
    DateTime? OrderDate);

public sealed record DashboardInventoryResponse(
    int LowStock,
    int OutOfStock,
    int LowStockThreshold,
    IReadOnlyCollection<DashboardProductResponse> AttentionProducts);

public sealed record DashboardProductResponse(
    Guid Id,
    Guid? VariantId,
    string Name,
    string? Sku,
    string? VariantCode,
    IReadOnlyCollection<DashboardProductVariantOptionResponse> Options,
    string? ImageUrl,
    int AvailableStock,
    ProductStatus Status,
    string Issue);

public sealed record DashboardProductVariantOptionResponse(
    string Name,
    string Value);

public sealed record DashboardPaymentsResponse(
    int Total,
    int Paid,
    int Pending,
    int PartialPayment,
    int ExcessPayment,
    int Voided);

public sealed record DashboardSyncResponse(
    int Succeeded,
    int Pending,
    int Failed,
    DateTime? LastSuccessfulAt);

public sealed record DashboardSyncRunResponse(
    Guid Id,
    string Type,
    string? BackgroundJobId,
    string Status,
    int TotalFetched,
    int Created,
    int Updated,
    int Unchanged,
    int Deactivated,
    int Failed,
    IReadOnlyCollection<string> Errors,
    DateTime RequestedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);
