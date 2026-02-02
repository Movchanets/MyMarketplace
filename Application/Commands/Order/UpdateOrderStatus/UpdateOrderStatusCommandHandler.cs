using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Order.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, ServiceResponse<OrderStatusDto>>
{
	private readonly IOrderRepository _orderRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

	public UpdateOrderStatusCommandHandler(
		IOrderRepository orderRepository,
		IUnitOfWork unitOfWork,
		ILogger<UpdateOrderStatusCommandHandler> logger)
	{
		_orderRepository = orderRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<OrderStatusDto>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating order {OrderId} status to {NewStatus}",
			request.OrderId, request.NewStatus);

		try
		{
			// Get order
			var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
			if (order is null)
			{
				_logger.LogWarning("Order {OrderId} not found", request.OrderId);
				return new ServiceResponse<OrderStatusDto>(false, "Order not found", null);
			}

			// Validate status transition
			if (!order.Status.IsValidTransition(request.NewStatus))
			{
				_logger.LogWarning("Invalid status transition from {CurrentStatus} to {NewStatus} for order {OrderId}",
					order.Status, request.NewStatus, request.OrderId);
				return new ServiceResponse<OrderStatusDto>(false,
					$"Cannot transition from {order.Status.GetDisplayName()} to {request.NewStatus.GetDisplayName()}", null);
			}

			// Update status
			order.UpdateStatus(request.NewStatus);

			// Set tracking info if provided (for shipped status)
			if (request.NewStatus == OrderStatus.Shipped &&
				!string.IsNullOrWhiteSpace(request.TrackingNumber) &&
				!string.IsNullOrWhiteSpace(request.ShippingCarrier))
			{
				order.SetTrackingInfo(request.TrackingNumber, request.ShippingCarrier);
			}

			_orderRepository.Update(order);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Order {OrderId} status updated to {NewStatus}",
				request.OrderId, request.NewStatus);

			var statusDto = new OrderStatusDto(
				order.Id,
				order.OrderNumber,
				order.Status,
				order.PaymentStatus,
				order.ShippedAt,
				order.DeliveredAt,
				order.TrackingNumber,
				order.ShippingCarrier
			);

			return new ServiceResponse<OrderStatusDto>(true, "Order status updated successfully", statusDto);
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogWarning(ex, "Invalid operation updating order {OrderId} status", request.OrderId);
			return new ServiceResponse<OrderStatusDto>(false, ex.Message, null);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating order {OrderId} status", request.OrderId);
			return new ServiceResponse<OrderStatusDto>(false, "An error occurred while updating order status", null);
		}
	}
}
