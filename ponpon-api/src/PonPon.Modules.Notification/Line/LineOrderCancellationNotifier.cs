using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Notification.Line;

public sealed class LineOrderCancellationNotifier : IOrderCancellationNotifier
{
    private const string PushEndpoint = "https://api.line.me/v2/bot/message/push";

    private readonly HttpClient _httpClient;
    private readonly LineNotificationOptions _options;
    private readonly ILogger<LineOrderCancellationNotifier> _logger;

    public LineOrderCancellationNotifier(
        HttpClient httpClient,
        IOptions<LineNotificationOptions> options,
        ILogger<LineOrderCancellationNotifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task NotifyCustomerCancellationCompletedAsync(
        OrderCancellationNotification notification,
        CancellationToken cancellationToken = default)
    {
        var message = BuildMessage(
            "ลูกค้ายกเลิกออเดอร์สำเร็จ",
            "ระบบดำเนินการคืนเงิน/ยกเลิกออเดอร์แล้ว",
            notification);

        return SendToAdminsAsync(message, cancellationToken);
    }

    public Task NotifyCustomerCancellationRequiresManualRefundAsync(
        OrderCancellationNotification notification,
        CancellationToken cancellationToken = default)
    {
        var message = BuildMessage(
            "ลูกค้าขอยกเลิกออเดอร์",
            "ต้องคืนเงิน manual ก่อนยกเลิกออเดอร์ เพราะ Omise refund อัตโนมัติไม่ได้",
            notification);

        return SendToAdminsAsync(message, cancellationToken);
    }

    private async Task SendToAdminsAsync(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ChannelAccessToken)
            || _options.AdminRecipientIds.Length == 0)
        {
            _logger.LogInformation(
                "LINE admin cancellation notification skipped because Line:ChannelAccessToken or Line:AdminRecipientIds is not configured.");
            return;
        }

        foreach (var recipientId in _options.AdminRecipientIds.Select(x => x.Trim()).Where(x => x.Length > 0))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, PushEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ChannelAccessToken);
                request.Content = JsonContent.Create(new
                {
                    to = recipientId,
                    messages = new[]
                    {
                        new
                        {
                            type = "text",
                            text = message
                        }
                    }
                });

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "LINE admin cancellation notification failed: StatusCode={StatusCode} Body={Body}",
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
                    "LINE admin cancellation notification failed for recipient {RecipientId}.",
                    recipientId);
            }
        }
    }

    private static string BuildMessage(
        string title,
        string action,
        OrderCancellationNotification notification)
    {
        var amount = notification.PaymentAmount.ToString("#,##0.00", CultureInfo.InvariantCulture);
        var customerName = string.IsNullOrWhiteSpace(notification.CustomerName)
            ? "-"
            : notification.CustomerName.Trim();
        var customerPhone = string.IsNullOrWhiteSpace(notification.CustomerPhone)
            ? "-"
            : notification.CustomerPhone.Trim();
        var chargeId = string.IsNullOrWhiteSpace(notification.OmiseChargeId)
            ? "-"
            : notification.OmiseChargeId.Trim();

        return string.Join(Environment.NewLine, new[]
        {
            title,
            $"Order: {notification.OrderNumber}",
            $"Amount: {amount}",
            $"Customer: {customerName}",
            $"Phone: {customerPhone}",
            $"PaymentStatus: {notification.PaymentStatus}",
            $"OmiseChargeId: {chargeId}",
            $"Reason: {notification.Reason.Trim()}",
            $"Action: {action}"
        });
    }
}
