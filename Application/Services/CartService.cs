using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Service providing common cart operations with standardized error handling, 
/// concurrency control, and DRY-compliant patterns for ACID compliance
/// </summary>
public sealed class CartService : ICartService
{
	private readonly ICartRepository _cartRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IProductRepository _productRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly ILogger<CartService> _logger;

	public CartService(
		ICartRepository cartRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		IProductRepository productRepository,
		ISkuRepository skuRepository,
		ILogger<CartService> logger)
	{
		_cartRepository = cartRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_productRepository = productRepository;
		_skuRepository = skuRepository;
		_logger = logger;
	}

	public async Task<CartOperationResult<(Cart Cart, User DomainUser, bool IsNewCart)>> GetOrCreateCartAsync(
		Guid userId,
		CancellationToken cancellationToken = default)
	{
		// Validate user
		var domainUser = await _userRepository.GetByIdentityUserIdAsync(userId);
		if (domainUser is null)
		{
			_logger.LogWarning("Domain user for identity {UserId} not found", userId);
			return CartOperationResult<(Cart, User, bool)>.Failure("User not found");
		}

		// Get or create cart
		var cart = await _cartRepository.GetByUserIdAsync(domainUser.Id, cancellationToken);
		var isNewCart = cart is null;

		if (cart is null)
		{
			cart = new Cart(domainUser.Id);
			_cartRepository.Add(cart);
			_logger.LogDebug("Created new cart for user {UserId}", domainUser.Id);
		}
		else
		{
			_logger.LogDebug(
				"Found existing cart {CartId} for user {UserId}. RowVersion: {RowVersion}, Items: {ItemCount}",
				cart.Id, domainUser.Id, cart.RowVersion != null ? Convert.ToHexString(cart.RowVersion) : "null", cart.Items.Count);
		}

		return CartOperationResult<(Cart, User, bool)>.Success((cart, domainUser, isNewCart));
	}

	public async Task<CartOperationResult<(Cart Cart, User DomainUser)>> GetCartAsync(
		Guid userId,
		CancellationToken cancellationToken = default)
	{
		// Validate user
		var domainUser = await _userRepository.GetByIdentityUserIdAsync(userId);
		if (domainUser is null)
		{
			_logger.LogWarning("Domain user for identity {UserId} not found", userId);
			return CartOperationResult<(Cart, User)>.Failure("User not found");
		}

		// Get cart
		var cart = await _cartRepository.GetByUserIdAsync(domainUser.Id, cancellationToken);
		if (cart is null)
		{
			_logger.LogWarning("Cart not found for user {UserId}", userId);
			return CartOperationResult<(Cart, User)>.Failure("Cart not found");
		}

		return CartOperationResult<(Cart, User)>.Success((cart, domainUser));
	}

	public async Task<CartOperationResult<(Cart Cart, User DomainUser)>> GetCartWithProductsAsync(
		Guid userId,
		CancellationToken cancellationToken = default)
	{
		// Validate user
		var domainUser = await _userRepository.GetByIdentityUserIdAsync(userId);
		if (domainUser is null)
		{
			_logger.LogWarning("Domain user for identity {UserId} not found", userId);
			return CartOperationResult<(Cart, User)>.Failure("User not found");
		}

		// Get cart with products loaded (for checkout/display scenarios)
		var cart = await _cartRepository.GetByUserIdWithProductsAsync(domainUser.Id, cancellationToken);
		if (cart is null)
		{
			_logger.LogWarning("Cart not found for user {UserId}", userId);
			return CartOperationResult<(Cart, User)>.Failure("Cart not found");
		}

		return CartOperationResult<(Cart, User)>.Success((cart, domainUser));
	}

	public CartOperationResult<bool> ValidateQuantity(int quantity)
	{
		if (quantity < CartConstants.MinQuantityPerItem)
		{
			return CartOperationResult<bool>.Failure($"Quantity must be at least {CartConstants.MinQuantityPerItem}");
		}

		if (quantity > CartConstants.MaxQuantityPerSku)
		{
			return CartOperationResult<bool>.Failure($"Maximum {CartConstants.MaxQuantityPerSku} items allowed per product variant");
		}

		return CartOperationResult<bool>.Success(true);
	}

	public CartOperationResult<int> ValidateTotalQuantity(int requestedQuantity, int existingQuantity)
	{
		var totalQuantity = existingQuantity + requestedQuantity;

		if (totalQuantity > CartConstants.MaxQuantityPerSku)
		{
			return CartOperationResult<int>.Failure(
				$"Maximum {CartConstants.MaxQuantityPerSku} items allowed per product variant");
		}

		return CartOperationResult<int>.Success(totalQuantity);
	}

	public CartDto MapToCartDto(Cart cart)
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

	public async Task<ServiceResponse<CartDto>> AddOrUpdateItemAsync(Guid userId, Guid productId, Guid skuId, int quantity, CancellationToken cancellationToken = default)
	{
		// Validate quantity
		var quantityValidation = ValidateQuantity(quantity);
		if (!quantityValidation.IsSuccess)
		{
			return new ServiceResponse<CartDto>(false, quantityValidation.ErrorMessage!, null);
		}

		// Ensure user and cart
		var cartResult = await GetOrCreateCartAsync(userId, cancellationToken);
		if (!cartResult.IsSuccess)
		{
			return new ServiceResponse<CartDto>(false, cartResult.ErrorMessage ?? "User not found", null);
		}

		var (cart, domainUser, _) = cartResult.Data;

		// Validate product and SKU
		var product = await _productRepository.GetByIdAsync(productId);
		if (product is null || !product.IsActive)
		{
			return new ServiceResponse<CartDto>(false, "Product not found or not active", null);
		}

		var sku = await _skuRepository.GetByIdAsync(skuId);
		if (sku is null || sku.ProductId != productId)
		{
			return new ServiceResponse<CartDto>(false, "Product variant not found", null);
		}

		// Validate total quantity against inventory
		var existing = await _cartRepository.GetCartItemByCartIdAndSkuAsync(cart.Id, skuId, cancellationToken);
		var existingQuantity = existing?.Quantity ?? 0;
		var totalQuantityResult = ValidateTotalQuantity(quantity, existingQuantity);
		if (!totalQuantityResult.IsSuccess)
		{
			return new ServiceResponse<CartDto>(false, totalQuantityResult.ErrorMessage!, null);
		}

		var totalQuantity = totalQuantityResult.Data;
		if (sku.StockQuantity < totalQuantity)
		{
			return new ServiceResponse<CartDto>(false,
				$"Insufficient stock. You already have {existingQuantity} in cart. Only {sku.StockQuantity} items available.",
				null);
		}

		// Transactional update
		return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
		{
			var existingInTx = await _cartRepository.GetCartItemByCartIdAndSkuAsync(cart.Id, skuId, ct);
			if (existingInTx != null)
			{
				existingInTx.UpdateQuantity(existingInTx.Quantity + quantity);
				_cartRepository.UpdateCartItem(existingInTx);
			}
			else
			{
				var newItem = new CartItem(cart.Id, productId, skuId, quantity);
				_cartRepository.AddCartItem(newItem);
			}

			await _unitOfWork.SaveChangesAsync(ct);

			var refreshedCart = await _cartRepository.GetByUserIdWithProductsAsync(domainUser.Id, ct);
			var dto = MapToCartDto(refreshedCart!);
			return new ServiceResponse<CartDto>(true, "Item added to cart", dto);
		}, cancellationToken);
	}
}
