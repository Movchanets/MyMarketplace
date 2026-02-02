using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Cart.AddToCart;

/// <summary>
/// Command to add a product SKU to the user's cart
/// </summary>
public sealed record AddToCartCommand(
	Guid UserId,
	Guid ProductId,
	Guid SkuId,
	int Quantity
) : IRequest<ServiceResponse<CartDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}"];
}

/// <summary>
/// DTO representing the cart after an operation
/// </summary>
public record CartDto(
	Guid Id,
	Guid UserId,
	List<CartItemDto> Items,
	int TotalItems,
	decimal TotalPrice
);

/// <summary>
/// DTO representing a cart item
/// </summary>
public record CartItemDto(
	Guid Id,
	Guid ProductId,
	string ProductName,
	string? ProductImageUrl,
	Guid SkuId,
	string SkuCode,
	string? SkuAttributes,
	int Quantity,
	decimal UnitPrice,
	decimal Subtotal,
	DateTime AddedAt
);
