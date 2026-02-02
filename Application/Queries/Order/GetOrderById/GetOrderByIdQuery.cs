using Application.Commands.Order.CreateOrder;
using Application.DTOs;
using Domain.ValueObjects;
using MediatR;

namespace Application.Queries.Order.GetOrderById;

/// <summary>
/// Query to get a specific order by ID with full details
/// </summary>
public sealed record GetOrderByIdQuery(
	Guid OrderId,
	Guid UserId
) : IRequest<ServiceResponse<OrderDetailDto>>;

/// <summary>
/// Detailed DTO for order view
/// </summary>
public record OrderDetailDto(
	Guid Id,
	string OrderNumber,
	Guid UserId,
	List<OrderItemDto> Items,
	decimal TotalPrice,
	decimal Subtotal,
	decimal ShippingCost,
	decimal DiscountAmount,
	string Status,
	string PaymentStatus,
	ShippingAddressDto ShippingAddress,
	string DeliveryMethod,
	string PaymentMethod,
	string? PromoCode,
	string? CustomerNotes,
	DateTime CreatedAt,
	DateTime? UpdatedAt,
	DateTime? ShippedAt,
	DateTime? DeliveredAt,
	DateTime? CancelledAt,
	string? CancellationReason,
	string? TrackingNumber,
	string? ShippingCarrier
);

/// <summary>
/// DTO for shipping address
/// </summary>
public record ShippingAddressDto(
	string FirstName,
	string LastName,
	string PhoneNumber,
	string Email,
	string AddressLine1,
	string? AddressLine2,
	string City,
	string? State,
	string PostalCode,
	string Country,
	string FullName,
	string FormattedAddress
);
