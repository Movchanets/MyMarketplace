using Application.Behaviors;
using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Cart.UpdateCartQuantity;

/// <summary>
/// Command to update the quantity of a cart item
/// </summary>
public sealed record UpdateCartQuantityCommand(
	Guid UserId,
	Guid CartItemId,
	int Quantity
) : IRequest<ServiceResponse<CartDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}"];
}

/// <summary>
/// Command to update cart item quantity by SKU ID
/// </summary>
public sealed record UpdateCartQuantityBySkuCommand(
	Guid UserId,
	Guid SkuId,
	int Quantity
) : IRequest<ServiceResponse<CartDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}"];
}
