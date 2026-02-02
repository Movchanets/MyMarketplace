using Application.Behaviors;
using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Cart.RemoveFromCart;

/// <summary>
/// Command to remove an item from the user's cart
/// </summary>
public sealed record RemoveFromCartCommand(
	Guid UserId,
	Guid CartItemId
) : IRequest<ServiceResponse<CartDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}"];
}

/// <summary>
/// Command to remove an item from cart by SKU ID
/// </summary>
public sealed record RemoveFromCartBySkuCommand(
	Guid UserId,
	Guid SkuId
) : IRequest<ServiceResponse<CartDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}"];
}
