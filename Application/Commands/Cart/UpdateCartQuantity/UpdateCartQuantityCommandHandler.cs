using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Cart.UpdateCartQuantity;

public sealed class UpdateCartQuantityCommandHandler : IRequestHandler<UpdateCartQuantityCommand, ServiceResponse<CartDto>>
{
	private readonly ICartService _cartService;
	private readonly ICartRepository _cartRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<UpdateCartQuantityCommandHandler> _logger;

	public UpdateCartQuantityCommandHandler(
		ICartService cartService,
		ICartRepository cartRepository,
		ISkuRepository skuRepository,
		IUnitOfWork unitOfWork,
		ILogger<UpdateCartQuantityCommandHandler> logger)
	{
		_cartService = cartService;
		_cartRepository = cartRepository;
		_skuRepository = skuRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<CartDto>> Handle(UpdateCartQuantityCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating quantity for cart item {CartItemId} to {Quantity} for user {UserId}",
			request.CartItemId, request.Quantity, request.UserId);

		try
		{
			// Validate quantity constraints
			var quantityValidation = _cartService.ValidateQuantity(request.Quantity);
			if (!quantityValidation.IsSuccess)
			{
				return new ServiceResponse<CartDto>(false, quantityValidation.ErrorMessage!, null);
			}

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

			// Inventory validation
			var inventoryValidation = await ValidateInventoryAsync(cartItem.SkuId, request.Quantity);
			if (!inventoryValidation.IsValid)
			{
				return new ServiceResponse<CartDto>(false, inventoryValidation.ErrorMessage!, null);
			}

			// Update quantity (EF Core change tracking automatically detects modifications)
			cart.UpdateItemQuantity(request.CartItemId, request.Quantity);

			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Updated quantity for cart item {CartItemId} to {Quantity} for user {UserId}",
				request.CartItemId, request.Quantity, request.UserId);

			var cartDto = _cartService.MapToCartDto(cart);
			return new ServiceResponse<CartDto>(true, "Quantity updated", cartDto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating cart quantity for user {UserId}", request.UserId);
			return new ServiceResponse<CartDto>(false, "An error occurred while updating quantity", null);
		}
	}

	private async Task<(bool IsValid, string? ErrorMessage)> ValidateInventoryAsync(Guid skuId, int quantity)
	{
		var sku = await _skuRepository.GetByIdAsync(skuId);
		if (sku is null)
		{
			_logger.LogWarning("SKU {SkuId} not found", skuId);
			return (false, "Product variant not found");
		}

		if (sku.StockQuantity < quantity)
		{
			_logger.LogWarning("Insufficient inventory for SKU {SkuId}. Requested: {Requested}, Available: {Available}",
				skuId, quantity, sku.StockQuantity);
			return (false, $"Insufficient stock. Only {sku.StockQuantity} items available.");
		}

		return (true, null);
	}
}

public sealed class UpdateCartQuantityBySkuCommandHandler : IRequestHandler<UpdateCartQuantityBySkuCommand, ServiceResponse<CartDto>>
{
	private readonly ICartService _cartService;
	private readonly ICartRepository _cartRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<UpdateCartQuantityBySkuCommandHandler> _logger;

	public UpdateCartQuantityBySkuCommandHandler(
		ICartService cartService,
		ICartRepository cartRepository,
		ISkuRepository skuRepository,
		IUnitOfWork unitOfWork,
		ILogger<UpdateCartQuantityBySkuCommandHandler> logger)
	{
		_cartService = cartService;
		_cartRepository = cartRepository;
		_skuRepository = skuRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<CartDto>> Handle(UpdateCartQuantityBySkuCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating quantity for SKU {SkuId} to {Quantity} for user {UserId}",
			request.SkuId, request.Quantity, request.UserId);

		try
		{
			// Validate quantity constraints
			var quantityValidation = _cartService.ValidateQuantity(request.Quantity);
			if (!quantityValidation.IsSuccess)
			{
				return new ServiceResponse<CartDto>(false, quantityValidation.ErrorMessage!, null);
			}

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

			// Inventory validation
			var inventoryValidation = await ValidateInventoryAsync(request.SkuId, request.Quantity);
			if (!inventoryValidation.IsValid)
			{
				return new ServiceResponse<CartDto>(false, inventoryValidation.ErrorMessage!, null);
			}

			// Update quantity by SKU (EF Core change tracking automatically detects modifications)
			cart.UpdateItemQuantityBySku(request.SkuId, request.Quantity);

			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Updated quantity for SKU {SkuId} to {Quantity} for user {UserId}",
				request.SkuId, request.Quantity, request.UserId);

			var cartDto = _cartService.MapToCartDto(cart);
			return new ServiceResponse<CartDto>(true, "Quantity updated", cartDto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating cart quantity for user {UserId}", request.UserId);
			return new ServiceResponse<CartDto>(false, "An error occurred while updating quantity", null);
		}
	}

	private async Task<(bool IsValid, string? ErrorMessage)> ValidateInventoryAsync(Guid skuId, int quantity)
	{
		var sku = await _skuRepository.GetByIdAsync(skuId);
		if (sku is null)
		{
			_logger.LogWarning("SKU {SkuId} not found", skuId);
			return (false, "Product variant not found");
		}

		if (sku.StockQuantity < quantity)
		{
			_logger.LogWarning("Insufficient inventory for SKU {SkuId}. Requested: {Requested}, Available: {Available}",
				skuId, quantity, sku.StockQuantity);
			return (false, $"Insufficient stock. Only {sku.StockQuantity} items available.");
		}

		return (true, null);
	}
}
