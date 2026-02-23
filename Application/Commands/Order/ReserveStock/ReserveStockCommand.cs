using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Order.ReserveStock;

/// <summary>
/// Command to reserve stock for all items in the user's cart when proceeding to checkout.
/// Creates StockReservation records that hold inventory for a limited time (default 15 min).
/// </summary>
public sealed record ReserveStockCommand(
	Guid UserId,
	string? SessionId = null,
	string? IpAddress = null,
	string? UserAgent = null
) : IRequest<ServiceResponse<StockReservationResultDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}"];
}

/// <summary>
/// Result of a stock reservation attempt
/// </summary>
public record StockReservationResultDto(
	Guid CartId,
	List<ReservedItemDto> ReservedItems,
	DateTime ExpiresAt,
	int TotalReservedItems
);

/// <summary>
/// Details of a single reserved SKU
/// </summary>
public record ReservedItemDto(
	Guid ReservationId,
	Guid SkuId,
	string SkuCode,
	int Quantity,
	DateTime ExpiresAt
);
