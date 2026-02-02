using Application.DTOs;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Order.GetOrderStatusHistory;

public sealed class GetOrderStatusHistoryQueryHandler : IRequestHandler<GetOrderStatusHistoryQuery, ServiceResponse<OrderStatusHistoryResult>>
{
	private readonly IOrderRepository _orderRepository;
	private readonly IUserRepository _userRepository;
	private readonly ILogger<GetOrderStatusHistoryQueryHandler> _logger;

	public GetOrderStatusHistoryQueryHandler(
		IOrderRepository orderRepository,
		IUserRepository userRepository,
		ILogger<GetOrderStatusHistoryQueryHandler> logger)
	{
		_orderRepository = orderRepository;
		_userRepository = userRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<OrderStatusHistoryResult>> Handle(GetOrderStatusHistoryQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Getting status history for order {OrderId}", request.OrderId);

		try
		{
			// Validate user
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<OrderStatusHistoryResult>(false, "User not found", null);
			}

			// Get order
			var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
			if (order is null)
			{
				_logger.LogWarning("Order {OrderId} not found", request.OrderId);
				return new ServiceResponse<OrderStatusHistoryResult>(false, "Order not found", null);
			}

			// Verify order belongs to user
			if (order.UserId != domainUser.Id)
			{
				_logger.LogWarning("Order {OrderId} does not belong to user {UserId}",
					request.OrderId, request.UserId);
				return new ServiceResponse<OrderStatusHistoryResult>(false, "Order not found", null);
			}

			// Build status history based on order timestamps
			var history = BuildStatusHistory(order);

			var result = new OrderStatusHistoryResult(
				order.Id,
				order.OrderNumber,
				order.Status.ToString(),
				order.PaymentStatus.ToString(),
				history
			);

			return new ServiceResponse<OrderStatusHistoryResult>(true, "Status history retrieved successfully", result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting status history for order {OrderId}", request.OrderId);
			return new ServiceResponse<OrderStatusHistoryResult>(false, "An error occurred while retrieving status history", null);
		}
	}

	private static List<StatusHistoryEntry> BuildStatusHistory(Domain.Entities.Order order)
	{
		var history = new List<StatusHistoryEntry>();
		var currentStatus = order.Status;

		// Order Created (always present)
		history.Add(new StatusHistoryEntry(
			OrderStatus.Pending.ToString(),
			"Order placed successfully",
			order.CreatedAt,
			currentStatus == OrderStatus.Pending
		));

		// Confirmed
		if (currentStatus >= OrderStatus.Confirmed || order.UpdatedAt > order.CreatedAt)
		{
			history.Add(new StatusHistoryEntry(
				OrderStatus.Confirmed.ToString(),
				"Order confirmed",
				order.CreatedAt.AddMinutes(5), // Approximate
				currentStatus == OrderStatus.Confirmed
			));
		}

		// Processing
		if (currentStatus >= OrderStatus.Processing)
		{
			history.Add(new StatusHistoryEntry(
				OrderStatus.Processing.ToString(),
				"Order is being prepared",
				order.CreatedAt.AddHours(1), // Approximate
				currentStatus == OrderStatus.Processing
			));
		}

		// Shipped
		if (currentStatus >= OrderStatus.Shipped && order.ShippedAt.HasValue)
		{
			history.Add(new StatusHistoryEntry(
				OrderStatus.Shipped.ToString(),
				$"Order shipped{(string.IsNullOrEmpty(order.TrackingNumber) ? "" : $" with tracking number {order.TrackingNumber}")}",
				order.ShippedAt.Value,
				currentStatus == OrderStatus.Shipped
			));
		}

		// Delivered
		if (currentStatus >= OrderStatus.Delivered && order.DeliveredAt.HasValue)
		{
			history.Add(new StatusHistoryEntry(
				OrderStatus.Delivered.ToString(),
				"Order delivered successfully",
				order.DeliveredAt.Value,
				currentStatus == OrderStatus.Delivered
			));
		}

		// Cancelled
		if (currentStatus == OrderStatus.Cancelled && order.CancelledAt.HasValue)
		{
			history.Add(new StatusHistoryEntry(
				OrderStatus.Cancelled.ToString(),
				$"Order cancelled{(string.IsNullOrEmpty(order.CancellationReason) ? "" : $": {order.CancellationReason}")}",
				order.CancelledAt.Value,
				true
			));
		}

		return history;
	}
}
