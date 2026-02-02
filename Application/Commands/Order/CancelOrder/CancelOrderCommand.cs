using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Order.CancelOrder;

/// <summary>
/// Command to cancel an order with inventory restoration
/// </summary>
public sealed record CancelOrderCommand(
	Guid OrderId,
	Guid UserId,
	string? Reason = null
) : IRequest<ServiceResponse<bool>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"orders:{UserId}", "orders"];
}
