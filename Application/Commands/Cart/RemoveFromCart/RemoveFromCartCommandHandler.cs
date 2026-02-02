using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Cart.RemoveFromCart;

public sealed class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, ServiceResponse<CartDto>>
{
	private readonly ICartService _cartService;
	private readonly ICartRepository _cartRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<RemoveFromCartCommandHandler> _logger;

	public RemoveFromCartCommandHandler(
		ICartService cartService,
		ICartRepository cartRepository,
		IUnitOfWork unitOfWork,
		ILogger<RemoveFromCartCommandHandler> logger)
	{
		_cartService = cartService;
		_cartRepository = cartRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<CartDto>> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Removing cart item {CartItemId} for user {UserId}",
			request.CartItemId, request.UserId);

		try
		{
			// Get cart
			var cartResult = await _cartService.GetCartAsync(request.UserId, cancellationToken);
			if (!cartResult.IsSuccess)
			{
				return cartResult.ToFailureResponse<CartDto>();
			}

			var (cart, _) = cartResult.Data;

			// Verify the item belongs to this cart
			var cartItem = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
			if (cartItem is null)
			{
				_logger.LogWarning("Cart item {CartItemId} not found in cart for user {UserId}",
					request.CartItemId, request.UserId);
				return new ServiceResponse<CartDto>(false, "Item not found in cart", null);
			}

			// Remove item (EF Core change tracking automatically detects modifications)
			cart.RemoveItem(request.CartItemId);

			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Removed cart item {CartItemId} for user {UserId}",
				request.CartItemId, request.UserId);

			var cartDto = _cartService.MapToCartDto(cart);
			return new ServiceResponse<CartDto>(true, "Item removed from cart", cartDto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error removing item from cart for user {UserId}", request.UserId);
			return new ServiceResponse<CartDto>(false, "An error occurred while removing item from cart", null);
		}
	}
}

public sealed class RemoveFromCartBySkuCommandHandler : IRequestHandler<RemoveFromCartBySkuCommand, ServiceResponse<CartDto>>
{
	private readonly ICartService _cartService;
	private readonly ICartRepository _cartRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<RemoveFromCartBySkuCommandHandler> _logger;

	public RemoveFromCartBySkuCommandHandler(
		ICartService cartService,
		ICartRepository cartRepository,
		IUnitOfWork unitOfWork,
		ILogger<RemoveFromCartBySkuCommandHandler> logger)
	{
		_cartService = cartService;
		_cartRepository = cartRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<CartDto>> Handle(RemoveFromCartBySkuCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Removing SKU {SkuId} from cart for user {UserId}",
			request.SkuId, request.UserId);

		try
		{
			// Get cart
			var cartResult = await _cartService.GetCartAsync(request.UserId, cancellationToken);
			if (!cartResult.IsSuccess)
			{
				return cartResult.ToFailureResponse<CartDto>();
			}

			var (cart, _) = cartResult.Data;

			// Verify the item exists in cart
			if (!cart.ContainsSku(request.SkuId))
			{
				_logger.LogWarning("SKU {SkuId} not found in cart for user {UserId}",
					request.SkuId, request.UserId);
				return new ServiceResponse<CartDto>(false, "Item not found in cart", null);
			}

			// Remove item by SKU (EF Core change tracking automatically detects modifications)
			cart.RemoveItemBySku(request.SkuId);

			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Removed SKU {SkuId} from cart for user {UserId}",
				request.SkuId, request.UserId);

			var cartDto = _cartService.MapToCartDto(cart);
			return new ServiceResponse<CartDto>(true, "Item removed from cart", cartDto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error removing item from cart for user {UserId}", request.UserId);
			return new ServiceResponse<CartDto>(false, "An error occurred while removing item from cart", null);
		}
	}
}
