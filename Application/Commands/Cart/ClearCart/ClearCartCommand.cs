using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Cart.ClearCart;

/// <summary>
/// Command to clear all items from the user's cart
/// </summary>
public sealed record ClearCartCommand(
	Guid UserId
) : IRequest<ServiceResponse<bool>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}"];
}
