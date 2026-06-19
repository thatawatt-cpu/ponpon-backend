using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;

public sealed class AddOrderHandler
{
    private readonly IZortOrderClient _zortClient;
    private readonly IOrderRepository _orders;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AddOrderHandler(
        IZortOrderClient zortClient,
        IOrderRepository orders,
        IOrderingUnitOfWork unitOfWork,
        IProductRepository products,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _zortClient = zortClient;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _products = products;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<AddOrderResponse> HandleAsync(
        AddOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        Validate(command);

        var requestedItems = command.Items
            .GroupBy(x => new { x.ProductId, x.VariantId })
            .Select(x => new AddOrderItemCommand(x.Key.ProductId, x.Key.VariantId, x.Sum(item => item.Quantity)))
            .ToArray();

        var zortItems = new List<ZortAddOrderItemRequest>(requestedItems.Length);
        foreach (var requestedItem in requestedItems)
        {
            var product = await _products.GetByIdWithVariantsAsync(requestedItem.ProductId, cancellationToken)
                ?? throw new BadRequestException($"Product {requestedItem.ProductId} was not found.");

            if (!product.IsVisibleToCustomer)
            {
                throw new BadRequestException($"Product {product.Name} is not available.");
            }

            var variant = ResolveVariant(product, requestedItem.VariantId);
            if (requestedItem.Quantity > variant.AvailableStock)
            {
                throw new BadRequestException($"Product {product.Name} ({variant.Sku}) has insufficient stock.");
            }

            zortItems.Add(new ZortAddOrderItemRequest(
                variant.Sku,
                product.Name,
                requestedItem.Quantity,
                variant.SellPrice,
                0,
                variant.SellPrice * requestedItem.Quantity));
        }

        var itemAmount = zortItems.Sum(x => x.TotalPrice);
        var totalAmount = itemAmount + command.ShippingAmount;
        var number = $"LIFF-{command.ClientRequestId:N}";
        var addRequest = new ZortAddOrderRequest(
            number,
            command.ClientRequestId.ToString("N"),
            command.CustomerName.Trim(),
            EmptyToNull(command.CustomerEmail),
            command.CustomerPhone.Trim(),
            command.CustomerAddress.Trim(),
            command.ShippingName.Trim(),
            command.ShippingPhone.Trim(),
            command.ShippingAddress.Trim(),
            EmptyToNull(command.ShippingChannel),
            command.ShippingAmount,
            EmptyToNull(command.Description),
            SyncOrdersFromZort.SyncOrdersFromZortHandler.LineLiffSalesChannel,
            totalAmount,
            0,
            0,
            zortItems);

        var zortOrderId = await _zortClient.AddOrderAsync(addRequest, cancellationToken);
        var zortOrder = await _zortClient.GetOrderDetailAsync(zortOrderId, cancellationToken);
        var snapshot = ZortOrderMapper.ToSnapshot(zortOrder) with
        {
            SalesChannel = SyncOrdersFromZort.SyncOrdersFromZortHandler.LineLiffSalesChannel,
            IntegrationCustomerId = _currentUser.LineUserId,
            IntegrationCustomer = command.CustomerName.Trim()
        };

        var order = await _orders.GetByZortOrderIdAsync(zortOrderId, cancellationToken);
        if (order is null)
        {
            order = Order.CreateFromZort(snapshot, _clock.UtcNow);
            await _orders.AddAsync(order, cancellationToken);
        }
        else
        {
            order.ApplyZortSnapshot(snapshot, _clock.UtcNow);
        }

        order.AssignCustomer(customerId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddOrderResponse(
            order.Id,
            order.ZortOrderId,
            order.Number,
            order.Status,
            order.PaymentStatus,
            order.Amount);
    }

    private Guid GetCustomerId()
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserType != "Customer"
            || _currentUser.CustomerId is not Guid customerId)
        {
            throw new UnauthorizedException("Customer authentication is required.");
        }

        return customerId;
    }

    private static void Validate(AddOrderCommand command)
    {
        if (command.ClientRequestId == Guid.Empty)
        {
            throw new BadRequestException("ClientRequestId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.CustomerName)
            || string.IsNullOrWhiteSpace(command.CustomerPhone)
            || string.IsNullOrWhiteSpace(command.CustomerAddress)
            || string.IsNullOrWhiteSpace(command.ShippingName)
            || string.IsNullOrWhiteSpace(command.ShippingPhone)
            || string.IsNullOrWhiteSpace(command.ShippingAddress))
        {
            throw new BadRequestException("Customer and shipping information are required.");
        }

        if (command.ShippingAmount < 0)
        {
            throw new BadRequestException("ShippingAmount cannot be negative.");
        }

        if (command.Items.Count == 0 || command.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
        {
            throw new BadRequestException("At least one valid order item is required.");
        }
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ProductVariant ResolveVariant(Product product, Guid? variantId)
    {
        if (variantId is Guid id)
        {
            return product.FindVariant(id)
                ?? throw new BadRequestException($"Variant {id} was not found for product {product.Id}.");
        }

        var visibleVariants = product.Variants.Where(x => x.IsVisibleToCustomer).ToArray();
        if (visibleVariants.Length == 1)
        {
            return visibleVariants[0];
        }

        if (visibleVariants.Length == 0)
        {
            throw new BadRequestException($"Product {product.Name} has no available variants.");
        }

        throw new BadRequestException($"Product {product.Name} requires a variantId.");
    }
}
