using Application.Behaviors;
using Application.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Commands.Order.UpdateOrderStatus;

/// <summary>
/// Command to update the status of an order
/// </summary>
public sealed record UpdateOrderStatusCommand(
	Guid OrderId,
	OrderStatus NewStatus,
	string? TrackingNumber = null,
	string? ShippingCarrier = null
) : IRequest<ServiceResponse<OrderStatusDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["orders"];
}

/// <summary>
/// DTO representing order status update result
/// </summary>
public record OrderStatusDto(
	Guid OrderId,
	string OrderNumber,
	OrderStatus Status,
	PaymentStatus PaymentStatus,
	DateTime? ShippedAt,
	DateTime? DeliveredAt,
	string? TrackingNumber,
	string? ShippingCarrier
);
