using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Cart.GetUserCart;

public sealed class GetUserCartQueryHandler : IRequestHandler<GetUserCartQuery, ServiceResponse<CartDto>>
{
	private readonly ICartRepository _cartRepository;
	private readonly IUserRepository _userRepository;
	private readonly ILogger<GetUserCartQueryHandler> _logger;

	public GetUserCartQueryHandler(
		ICartRepository cartRepository,
		IUserRepository userRepository,
		ILogger<GetUserCartQueryHandler> logger)
	{
		_cartRepository = cartRepository;
		_userRepository = userRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<CartDto>> Handle(GetUserCartQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Getting cart for user {UserId}", request.UserId);

		try
		{
			// Validate user
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<CartDto>(false, "User not found", null);
			}

			// Get cart with items
			var cart = await _cartRepository.GetByUserIdAsync(domainUser.Id, cancellationToken);

			if (cart is null)
			{
				// Return empty cart
				var emptyCartDto = new CartDto(
					Guid.Empty,
					domainUser.Id,
					new List<CartItemDto>(),
					0,
					0
				);
				return new ServiceResponse<CartDto>(true, "Cart is empty", emptyCartDto);
			}

			var cartDto = MapToCartDto(cart);
			return new ServiceResponse<CartDto>(true, "Cart retrieved successfully", cartDto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting cart for user {UserId}", request.UserId);
			return new ServiceResponse<CartDto>(false, "An error occurred while retrieving cart", null);
		}
	}

	private static CartDto MapToCartDto(Domain.Entities.Cart cart)
	{
		var items = cart.Items.Select(item =>
		{
			var product = item.Product;
			var sku = item.Sku;
			var unitPrice = sku?.Price ?? 0;

			return new CartItemDto(
				item.Id,
				item.ProductId,
				product?.Name ?? "Unknown Product",
				product?.BaseImageUrl,
				item.SkuId,
				sku?.SkuCode ?? "Unknown",
				sku?.Attributes?.RootElement.ToString(),
				item.Quantity,
				unitPrice,
				unitPrice * item.Quantity,
				item.AddedAt
			);
		}).ToList();

		var totalPrice = items.Sum(i => i.Subtotal);

		return new CartDto(
			cart.Id,
			cart.UserId,
			items,
			cart.GetTotalItems(),
			totalPrice
		);
	}
}
