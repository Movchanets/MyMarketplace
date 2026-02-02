using Application.DTOs;
using MediatR;

namespace Application.Queries.Order.GetOrderStatusHistory;

/// <summary>
/// Query to get the status history of an order
/// </summary>
public sealed record GetOrderStatusHistoryQuery(
	Guid OrderId,
	Guid UserId
) : IRequest<ServiceResponse<OrderStatusHistoryResult>>;

/// <summary>
/// Result containing order status history
/// </summary>
public record OrderStatusHistoryResult(
	Guid OrderId,
	string OrderNumber,
	string CurrentStatus,
	string PaymentStatus,
	List<StatusHistoryEntry> History
);

/// <summary>
/// Entry in the status history
/// </summary>
public record StatusHistoryEntry(
	string Status,
	string Description,
	DateTime Timestamp,
	bool IsCurrentStatus
);
