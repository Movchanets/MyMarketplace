using Application.Commands.Order.CreateOrder;
using Application.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Queries.Order.GetUserOrders;

/// <summary>
/// Query to get orders for a user with filtering, sorting, and pagination
/// </summary>
public sealed record GetUserOrdersQuery(
	Guid UserId,
	OrderStatus? Status = null,
	DateTime? FromDate = null,
	DateTime? ToDate = null,
	string? SortBy = null,
	bool SortDescending = true,
	int PageNumber = 1,
	int PageSize = 20
) : IRequest<ServiceResponse<PagedOrdersResult>>;

/// <summary>
/// Result containing paginated orders
/// </summary>
public record PagedOrdersResult(
	List<OrderSummaryDto> Orders,
	int TotalCount,
	int PageNumber,
	int PageSize,
	int TotalPages
);

/// <summary>
/// Summary DTO for order list views
/// </summary>
public record OrderSummaryDto(
	Guid Id,
	string OrderNumber,
	OrderStatus Status,
	PaymentStatus PaymentStatus,
	decimal TotalPrice,
	int TotalItems,
	DateTime CreatedAt,
	string? TrackingNumber,
	string? ShippingCarrier
);
