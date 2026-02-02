using Application.Behaviors;
using Application.DTOs;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands.Order.CreateOrder;

/// <summary>
/// Command to create a new order from the user's cart
/// </summary>
public sealed record CreateOrderCommand(
	Guid UserId,
	ShippingAddress ShippingAddress,
	string DeliveryMethod,
	string PaymentMethod,
	string? PromoCode,
	string? CustomerNotes,
	string? IdempotencyKey
) : IRequest<ServiceResponse<OrderDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}", $"orders:{UserId}"];
}

/// <summary>
/// DTO representing an order
/// </summary>
public record OrderDto(
	Guid Id,
	string OrderNumber,
	Guid UserId,
	List<OrderItemDto> Items,
	decimal TotalPrice,
	OrderStatus Status,
	PaymentStatus PaymentStatus,
	string DeliveryMethod,
	string PaymentMethod,
	DateTime CreatedAt,
	string? TrackingNumber,
	string? ShippingCarrier
);

/// <summary>
/// DTO representing an order item
/// </summary>
public record OrderItemDto(
	Guid Id,
	Guid ProductId,
	string ProductName,
	string? ProductImageUrl,
	Guid SkuId,
	string SkuCode,
	string? SkuAttributes,
	int Quantity,
	decimal PriceAtPurchase,
	decimal Subtotal
);
