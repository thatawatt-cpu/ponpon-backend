using System.Text.Json;
using PonPon.Shared.Domain;

namespace PonPon.Modules.Ordering.Domain.Returns;

public sealed class OrderReturnRequest : Entity, IAuditableEntity
{
    private OrderReturnRequest()
    {
        Reason = string.Empty;
        Status = OrderReturnRequestStatus.Requested;
        EvidenceImageUrlsJson = "[]";
    }

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Reason { get; private set; }
    public string Status { get; private set; }
    public string EvidenceImageUrlsJson { get; private set; }
    public string? AdminNote { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public static OrderReturnRequest Create(
        Guid orderId,
        Guid customerId,
        string reason,
        IReadOnlyCollection<string> evidenceImageUrls,
        DateTime now)
    {
        return new OrderReturnRequest
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerId = customerId,
            Reason = reason.Trim(),
            Status = OrderReturnRequestStatus.Requested,
            EvidenceImageUrlsJson = JsonSerializer.Serialize(evidenceImageUrls),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public IReadOnlyCollection<string> GetEvidenceImageUrls()
    {
        return JsonSerializer.Deserialize<string[]>(EvidenceImageUrlsJson) ?? [];
    }

    public void UpdateStatus(string status, string? adminNote, DateTime now)
    {
        Status = status;
        AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        UpdatedAtUtc = now;
    }
}

public static class OrderReturnRequestStatus
{
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Completed = "Completed";

    public static string? NormalizeAdminStatus(string status)
    {
        if (string.Equals(status, Approved, StringComparison.OrdinalIgnoreCase))
            return Approved;
        if (string.Equals(status, Rejected, StringComparison.OrdinalIgnoreCase))
            return Rejected;
        if (string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase))
            return Completed;
        return null;
    }
}
