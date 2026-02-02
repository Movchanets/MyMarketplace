using Application.Commands.Order.CreateOrder;
using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Order.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, ServiceResponse<OrderDetailDto>>
{
	private readonly IOrderRepository _orderRepository;
	private readonly IUserRepository _userRepository;
	private readonly ILogger<GetOrderByIdQueryHandler> _logger;

	public GetOrderByIdQueryHandler(
		IOrderRepository orderRepository,
		IUserRepository userRepository,
		ILogger<GetOrderByIdQueryHandler> logger)
	{
		_orderRepository = orderRepository;
		_userRepository = userRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<OrderDetailDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Getting order {OrderId} for user {UserId}",
			request.OrderId, request.UserId);

		try
		{
			// Validate user
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<OrderDetailDto>(false, "User not found", null);
			}

			// Get order
			var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
			if (order is null)
			{
				_logger.LogWarning("Order {OrderId} not found", request.OrderId);
				return new ServiceResponse<OrderDetailDto>(false, "Order not found", null);
			}

			// Verify order belongs to user
			if (order.UserId != domainUser.Id)
			{
				_logger.LogWarning("Order {OrderId} does not belong to user {UserId}",
					request.OrderId, request.UserId);
				return new ServiceResponse<OrderDetailDto>(false, "Order not found", null);
			}

			var orderDto = MapToOrderDetailDto(order);
			return new ServiceResponse<OrderDetailDto>(true, "Order retrieved successfully", orderDto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting order {OrderId}", request.OrderId);
			return new ServiceResponse<OrderDetailDto>(false, "An error occurred while retrieving order", null);
		}
	}

	private static OrderDetailDto MapToOrderDetailDto(Domain.Entities.Order order)
	{
		var items = order.Items.Select(item => new OrderItemDto(
			item.Id,
			item.ProductId,
			item.ProductNameSnapshot,
			item.ProductImageUrlSnapshot,
			item.SkuId,
			item.SkuCodeSnapshot,
			item.SkuAttributesSnapshot,
			item.Quantity,
			item.PriceAtPurchase,
			item.GetSubtotal()
		)).ToList();

		var subtotal = items.Sum(i => i.Subtotal);

		var shippingAddress = new ShippingAddressDto(
			order.ShippingAddress.FirstName,
			order.ShippingAddress.LastName,
			order.ShippingAddress.PhoneNumber,
			order.ShippingAddress.Email,
			order.ShippingAddress.AddressLine1,
			order.ShippingAddress.AddressLine2,
			order.ShippingAddress.City,
			order.ShippingAddress.State,
			order.ShippingAddress.PostalCode,
			order.ShippingAddress.Country,
			order.ShippingAddress.GetFullName(),
			order.ShippingAddress.GetFormattedAddress()
		);

		return new OrderDetailDto(
			order.Id,
			order.OrderNumber,
			order.UserId,
			items,
			order.TotalPrice,
			subtotal,
			order.ShippingCost,
			order.DiscountAmount,
			order.Status.ToString(),
			order.PaymentStatus.ToString(),
			shippingAddress,
			order.DeliveryMethod,
			order.PaymentMethod,
			order.PromoCode,
			order.CustomerNotes,
			order.CreatedAt,
			order.UpdatedAt,
			order.ShippedAt,
			order.DeliveredAt,
			order.CancelledAt,
			order.CancellationReason,
			order.TrackingNumber,
			order.ShippingCarrier
		);
	}
}
