using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Notification.Line;

public sealed class LineOrderNotificationService : ILineOrderNotificationService
{
    private const string PushEndpoint = "https://api.line.me/v2/bot/message/push";

    private readonly HttpClient _httpClient;
    private readonly LineNotificationOptions _options;
    private readonly IRuntimeSettingProvider _settings;
    private readonly ILogger<LineOrderNotificationService> _logger;

    public LineOrderNotificationService(
        HttpClient httpClient,
        IOptions<LineNotificationOptions> options,
        IRuntimeSettingProvider settings,
        ILogger<LineOrderNotificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _settings = settings;
        _logger = logger;
    }

    public Task NotifyOrderCreatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "สร้างคำสั่งซื้อแล้ว", "กรุณาชำระเงินภายในเวลาที่กำหนด", "#111827", "ดูคำสั่งซื้อ", cancellationToken);

    public Task NotifyPaymentCreatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "พร้อมชำระเงิน", "ระบบสร้างรายการชำระเงินแล้ว", "#2563EB", "ไปชำระเงิน", cancellationToken);

    public Task NotifyPaymentSucceededAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "ชำระเงินสำเร็จ", "ร้านค้าจะเริ่มดำเนินการคำสั่งซื้อของคุณ", "#16A34A", "ติดตามออเดอร์", "#E60012", cancellationToken);

    public Task NotifyPaymentExpiredAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "หมดเวลาชำระเงิน", "คำสั่งซื้อถูกยกเลิกเพราะไม่ได้ชำระเงินในเวลาที่กำหนด", "#6B7280", "ดูคำสั่งซื้อ", cancellationToken);

    public Task NotifyAutoRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "ยกเลิกและคืนเงินแล้ว", "ระบบดำเนินการคืนเงินและยกเลิกคำสั่งซื้อเรียบร้อยแล้ว", "#16A34A", "ดูคำสั่งซื้อ", cancellationToken);

    public Task NotifyManualRefundRequestedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "ได้รับคำขอยกเลิก", "ร้านค้าจะตรวจสอบและดำเนินการคืนเงินให้", "#D97706", "ดูคำสั่งซื้อ", cancellationToken);

    public Task NotifyManualRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "คืนเงินเรียบร้อยแล้ว", "ร้านค้าดำเนินการคืนเงินและยกเลิกคำสั่งซื้อเรียบร้อยแล้ว", "#16A34A", "ดูคำสั่งซื้อ", cancellationToken);

    public Task NotifyPackedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "กำลังเตรียมจัดส่ง", "ร้านค้าแพ็คสินค้าและกำลังเตรียมส่งให้ขนส่ง", "#7C3AED", "ดูคำสั่งซื้อ", cancellationToken);

    public Task NotifyShippingBookedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "สร้างรายการจัดส่งแล้ว", "ระบบสร้าง booking ขนส่งเรียบร้อยแล้ว", "#2563EB", "ติดตามพัสดุ", cancellationToken);

    public Task NotifyShippingStatusAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
    {
        var (title, subtitle, color) = notification.Status?.ToLowerInvariant() switch
        {
            "shipping" => ("พัสดุกำลังจัดส่ง", "พัสดุถูกรับเข้าระบบขนส่งแล้ว", "#2563EB"),
            "complete" or "completed" or "success" => ("จัดส่งสำเร็จ", "พัสดุถูกจัดส่งเรียบร้อยแล้ว", "#16A34A"),
            "problem" or "invalid" or "failed" => ("การจัดส่งมีปัญหา", "ร้านค้ากำลังตรวจสอบรายการจัดส่งนี้", "#DC2626"),
            var value when value?.StartsWith("return") == true => ("พัสดุกำลังตีกลับ", "พัสดุมีสถานะคืนกลับ ร้านค้ากำลังตรวจสอบ", "#D97706"),
            _ => ("อัปเดตสถานะจัดส่ง", "มีการอัปเดตสถานะพัสดุของคุณ", "#2563EB")
        };

        return SendAsync(notification, title, subtitle, color, "ติดตามพัสดุ", cancellationToken);
    }

    public Task NotifyReturnRequestedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "ได้รับคำขอคืนสินค้า", "ร้านค้าจะตรวจสอบคำขอและหลักฐานของคุณ", "#D97706", "ดูคำขอคืนสินค้า", cancellationToken);

    public Task NotifyReturnUpdatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "อัปเดตคำขอคืนสินค้า", $"สถานะคำขอคืนสินค้า: {notification.Status ?? "-"}", "#2563EB", "ดูคำขอคืนสินค้า", cancellationToken);

    public Task NotifyReturnRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default)
        => SendAsync(notification, "คืนเงินคำขอคืนสินค้าเรียบร้อยแล้ว", "ร้านค้าดำเนินการคืนเงินและปิดรายการคืนสินค้าเรียบร้อยแล้ว", "#16A34A", "ดูคำขอคืนสินค้า", cancellationToken);

    private async Task SendAsync(
        LineOrderNotification notification,
        string title,
        string subtitle,
        string color,
        string actionLabel,
        CancellationToken cancellationToken)
        => await SendAsync(notification, title, subtitle, color, actionLabel, color, cancellationToken);

    private async Task SendAsync(
        LineOrderNotification notification,
        string title,
        string subtitle,
        string color,
        string actionLabel,
        string buttonColor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(notification.LineUserId))
        {
            _logger.LogInformation(
                "LINE customer notification skipped because order {OrderNumber} has no LineUserId.",
                notification.OrderNumber);
            return;
        }

        var lineSettings = await ResolveSettingsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(lineSettings.ChannelAccessToken))
        {
            _logger.LogInformation("LINE customer notification skipped because Line:ChannelAccessToken is not configured.");
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, PushEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", lineSettings.ChannelAccessToken);
            request.Content = JsonContent.Create(new
            {
                to = notification.LineUserId.Trim(),
                messages = new[]
                {
                    new
                    {
                        type = "flex",
                        altText = title,
                        contents = CreateBubble(notification, title, subtitle, color, actionLabel, buttonColor, lineSettings.WebAppBaseUrl)
                    }
                }
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "LINE customer notification failed: OrderNumber={OrderNumber} StatusCode={StatusCode} Body={Body}",
                    notification.OrderNumber,
                    response.StatusCode,
                    body);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "LINE customer notification failed for order {OrderNumber}.",
                notification.OrderNumber);
        }
    }

    private object CreateBubble(
        LineOrderNotification notification,
        string title,
        string subtitle,
        string color,
        string actionLabel,
        string buttonColor,
        string? webAppBaseUrl)
    {
        var rows = new List<object>
        {
            Row("เลขออเดอร์", notification.OrderNumber),
            Row("ยอดเงิน", FormatAmount(notification.Amount))
        };

        if (!string.IsNullOrWhiteSpace(notification.PaymentMethod))
            rows.Add(Row("ช่องทาง", notification.PaymentMethod.Trim()));

        if (!string.IsNullOrWhiteSpace(notification.ShippingAddress))
            rows.Add(Row("ที่อยู่จัดส่ง", notification.ShippingAddress.Trim()));

        if (!string.IsNullOrWhiteSpace(notification.TrackingNumber))
            rows.Add(Row("Tracking", notification.TrackingNumber.Trim()));

        if (notification.ExpiresAtUtc is DateTime expiresAtUtc)
            rows.Add(Row("หมดเวลา", expiresAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)));

        if (!string.IsNullOrWhiteSpace(notification.Reason))
            rows.Add(Row("เหตุผล", notification.Reason.Trim()));

        var bodyContents = new List<object>
        {
            new { type = "text", text = title, weight = "bold", size = "lg", color },
            new { type = "text", text = subtitle, size = "sm", color = "#4B5563", wrap = true, margin = "sm" },
            new { type = "separator", margin = "lg" },
            new { type = "box", layout = "vertical", spacing = "sm", margin = "lg", contents = rows.ToArray() }
        };

        var actionUrl = ResolveActionUrl(notification, webAppBaseUrl);
        if (actionUrl is not null)
        {
            bodyContents.Add(new
            {
                type = "button",
                style = "primary",
                color = buttonColor,
                margin = "lg",
                action = new
                {
                    type = "uri",
                    label = actionLabel,
                    uri = actionUrl
                }
            });
        }

        return new
        {
            type = "bubble",
            size = "mega",
            body = new
            {
                type = "box",
                layout = "vertical",
                spacing = "md",
                contents = bodyContents.ToArray()
            }
        };
    }

    private static string? ResolveActionUrl(LineOrderNotification notification, string? webAppBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(notification.ActionUrl))
            return notification.ActionUrl.Trim();

        if (string.IsNullOrWhiteSpace(webAppBaseUrl))
            return null;

        return $"{webAppBaseUrl.TrimEnd('/')}/orders/{notification.OrderId:D}";
    }

    private async Task<ResolvedLineNotificationSettings> ResolveSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _settings.GetGroupAsync("Line", cancellationToken);
        return new ResolvedLineNotificationSettings(
            Get(settings, "ChannelAccessToken", _options.ChannelAccessToken),
            Get(settings, "WebAppBaseUrl", _options.WebAppBaseUrl));
    }

    private static string Get(IReadOnlyDictionary<string, string?> settings, string key, string fallback)
        => settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private sealed record ResolvedLineNotificationSettings(string ChannelAccessToken, string WebAppBaseUrl);

    private static object Row(string label, string value)
        => new
        {
            type = "box",
            layout = "horizontal",
            contents = new object[]
            {
                new { type = "text", text = label, size = "sm", color = "#6B7280", flex = 2 },
                new { type = "text", text = value, size = "sm", color = "#111827", wrap = true, flex = 4 }
            }
        };

    private static string FormatAmount(decimal amount)
        => amount.ToString("#,##0.00", CultureInfo.InvariantCulture) + " บาท";
}
