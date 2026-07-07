using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Returns;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.ReturnOrder;

public sealed class CreateOrderReturnRequestHandler
{
    private const int MaximumEvidenceFiles = 5;
    private const long MaximumFileSize = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private readonly IOrderRepository _orders;
    private readonly IOrderReturnRequestRepository _returnRequests;
    private readonly ISupabaseStorageService _storage;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly IDateTimeProvider _clock;

    public CreateOrderReturnRequestHandler(
        IOrderRepository orders,
        IOrderReturnRequestRepository returnRequests,
        ISupabaseStorageService storage,
        IOrderingUnitOfWork unitOfWork,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _returnRequests = returnRequests;
        _storage = storage;
        _unitOfWork = unitOfWork;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _clock = clock;
    }

    public async Task<OrderReturnRequestResponse> HandleAsync(
        CreateOrderReturnRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        var order = await _orders.GetCustomerOrderByIdAsync(
            command.OrderId,
            command.CustomerId,
            cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        if (!IsDelivered(order.Status))
        {
            throw new BadRequestException("Return can be requested only after the order has been delivered.");
        }

        if (await _returnRequests.GetByOrderIdAsync(order.Id, cancellationToken) is not null)
        {
            throw new BadRequestException("A return request already exists for this order.");
        }

        var imageUrls = new List<string>(command.EvidenceFiles.Count);
        foreach (var file in command.EvidenceFiles)
        {
            var extension = AllowedContentTypes[file.ContentType];
            var path = $"order-returns/{order.Id}/{Guid.NewGuid()}{extension}";
            imageUrls.Add(await _storage.UploadAsync(
                path,
                file.Content,
                file.ContentType,
                cancellationToken));
        }

        var returnRequest = OrderReturnRequest.Create(
            order.Id,
            command.CustomerId,
            command.Reason,
            imageUrls,
            _clock.UtcNow);
        await _returnRequests.AddAsync(returnRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _shopRealtimeNotifications.NotifyAsync(
            new ShopRealtimeNotification(
                order.CustomerId,
                order.IntegrationCustomerId,
                "return_requested",
                order.Id,
                order.Number,
                "ได้รับคำขอคืนสินค้า",
                "ร้านค้าจะตรวจสอบคำขอและหลักฐานของคุณ",
                order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
                returnRequest.Status),
            cancellationToken);

        return ToResponse(returnRequest);
    }

    internal static OrderReturnRequestResponse ToResponse(OrderReturnRequest request)
    {
        return new OrderReturnRequestResponse(
            request.Id,
            request.OrderId,
            request.Reason,
            request.Status,
            request.GetEvidenceImageUrls(),
            request.AdminNote,
            request.CreatedAtUtc,
            request.UpdatedAtUtc);
    }

    private static void Validate(CreateOrderReturnRequestCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 2000)
        {
            throw new BadRequestException("Return reason is required and must not exceed 2000 characters.");
        }

        if (command.EvidenceFiles.Count is < 1 or > MaximumEvidenceFiles)
        {
            throw new BadRequestException($"Return request requires 1-{MaximumEvidenceFiles} evidence images.");
        }

        if (command.EvidenceFiles.Any(x =>
                x.Length <= 0
                || x.Length > MaximumFileSize
                || !AllowedContentTypes.ContainsKey(x.ContentType)))
        {
            throw new BadRequestException(
                "Evidence images must be jpeg, png, or webp and no larger than 10 MB each.");
        }
    }

    private static bool IsDelivered(string status)
    {
        return string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "1", StringComparison.OrdinalIgnoreCase);
    }
}
