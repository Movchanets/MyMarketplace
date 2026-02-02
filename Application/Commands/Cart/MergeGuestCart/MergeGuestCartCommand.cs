using Application.Behaviors;
using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Cart.MergeGuestCart;

/// <summary>
/// Command to merge guest cart items into the authenticated user's cart in a single transaction.
/// </summary>
public sealed record MergeGuestCartCommand(
	Guid UserId,
	List<MergeCartItemDto> Items
) : IRequest<ServiceResponse<CartDto>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => [$"cart:{UserId}"];
}

public sealed record MergeCartItemDto(
	Guid ProductId,
	Guid SkuId,
	int Quantity
);
