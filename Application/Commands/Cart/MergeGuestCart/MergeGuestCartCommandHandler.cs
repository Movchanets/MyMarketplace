using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Cart.MergeGuestCart;

public sealed class MergeGuestCartCommandHandler : IRequestHandler<MergeGuestCartCommand, ServiceResponse<CartDto>>
{
	private readonly ICartService _cartService;
	private readonly ICartRepository _cartRepository;
	private readonly IProductRepository _productRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<MergeGuestCartCommandHandler> _logger;

	public MergeGuestCartCommandHandler(
		ICartService cartService,
		ICartRepository cartRepository,
		IProductRepository productRepository,
		ISkuRepository skuRepository,
		IUnitOfWork unitOfWork,
		ILogger<MergeGuestCartCommandHandler> logger)
	{
		_cartService = cartService;
		_cartRepository = cartRepository;
		_productRepository = productRepository;
		_skuRepository = skuRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<CartDto>> Handle(MergeGuestCartCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Merging {ItemCount} guest cart items for user {UserId}",
			request.Items.Count, request.UserId);

		try
		{
			// Validate items exist
			if (request.Items.Count == 0)
			{
				return new ServiceResponse<CartDto>(true, "No items to merge", null);
			}

			// Validate and deduplicate items
			var validatedItems = await ValidateAndDeduplicateItemsAsync(request.Items, cancellationToken);

			if (validatedItems.Count == 0)
			{
				_logger.LogWarning("No valid items to merge for user {UserId}", request.UserId);
				return new ServiceResponse<CartDto>(false, "No valid items to add to cart", null);
			}

			// Execute merge operation
			var result = await MergeItemsToCartAsync(request.UserId, validatedItems, cancellationToken);

			if (!result.IsSuccess)
			{
				return new ServiceResponse<CartDto>(false, result.Message, null);
			}

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error merging guest cart for user {UserId}", request.UserId);
			return new ServiceResponse<CartDto>(false, "An error occurred while merging cart items", null);
		}
	}

	private async Task<ServiceResponse<CartDto>> MergeItemsToCartAsync(
		Guid userId,
		Dictionary<Guid, MergeCartItemDto> validatedItems,
		CancellationToken cancellationToken)
	{
		// Get or create cart
		var cartResult = await _cartService.GetOrCreateCartAsync(userId, cancellationToken);
		if (!cartResult.IsSuccess)
		{
			return cartResult.ToFailureResponse<CartDto>();
		}

		var (cart, _, _) = cartResult.Data;

		// Add all validated items to cart
		foreach (var item in validatedItems.Values)
		{
			var currentQuantityInCart = cart.GetSkuQuantity(item.SkuId);
			var totalQuantity = currentQuantityInCart + item.Quantity;

			// Validate max quantity
			if (totalQuantity > CartConstants.MaxQuantityPerSku)
			{
				_logger.LogWarning(
					"Max quantity exceeded for SKU {SkuId}. Current: {Current}, Adding: {Adding}, Max: {Max}",
					item.SkuId, currentQuantityInCart, item.Quantity, CartConstants.MaxQuantityPerSku);
				totalQuantity = CartConstants.MaxQuantityPerSku;
			}

			// Validate inventory again
			var sku = await _skuRepository.GetByIdAsync(item.SkuId);
			if (sku != null && sku.StockQuantity < totalQuantity)
			{
				_logger.LogWarning("Insufficient stock for SKU {SkuId}. Setting to available: {Available}",
					item.SkuId, sku.StockQuantity);
				totalQuantity = Math.Max(currentQuantityInCart, sku.StockQuantity);
			}

			if (totalQuantity > currentQuantityInCart)
			{
				var quantityToAdd = totalQuantity - currentQuantityInCart;
				cart.AddItem(item.ProductId, item.SkuId, quantityToAdd);
				_logger.LogDebug("Added {Quantity} of SKU {SkuId} to cart", quantityToAdd, item.SkuId);
			}
		}

		// EF Core change tracking automatically detects modifications
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Successfully merged {ItemCount} items into cart for user {UserId}",
			validatedItems.Count, userId);

		var cartDto = _cartService.MapToCartDto(cart);
		return new ServiceResponse<CartDto>(true, $"Added {validatedItems.Count} items to cart", cartDto);
	}

	private async Task<Dictionary<Guid, MergeCartItemDto>> ValidateAndDeduplicateItemsAsync(
		ICollection<MergeCartItemDto> items,
		CancellationToken cancellationToken)
	{
		var validatedItems = new Dictionary<Guid, MergeCartItemDto>();

		foreach (var item in items)
		{
			if (item.Quantity <= 0)
			{
				_logger.LogWarning("Invalid quantity {Quantity} for SKU {SkuId}, skipping",
					item.Quantity, item.SkuId);
				continue;
			}

			// Validate product exists and is active
			var product = await _productRepository.GetByIdAsync(item.ProductId);
			if (product is null || !product.IsActive)
			{
				_logger.LogWarning("Product {ProductId} not found or inactive, skipping", item.ProductId);
				continue;
			}

			// Validate SKU exists and belongs to product
			var sku = await _skuRepository.GetByIdAsync(item.SkuId);
			if (sku is null || sku.ProductId != item.ProductId)
			{
				_logger.LogWarning("SKU {SkuId} invalid or doesn't belong to product {ProductId}, skipping",
					item.SkuId, item.ProductId);
				continue;
			}

			// Check inventory and adjust quantity
			var adjustedQuantity = item.Quantity;
			if (sku.StockQuantity < item.Quantity)
			{
				_logger.LogWarning(
					"Insufficient stock for SKU {SkuId}. Requested: {Requested}, Available: {Available}",
					item.SkuId, item.Quantity, sku.StockQuantity);
				adjustedQuantity = Math.Min(item.Quantity, sku.StockQuantity);
				if (adjustedQuantity <= 0)
				{
					continue;
				}
			}

			// Merge quantities for duplicate SKUs
			if (validatedItems.TryGetValue(item.SkuId, out var existingItem))
			{
				var totalQuantity = existingItem.Quantity + adjustedQuantity;
				if (totalQuantity > CartConstants.MaxQuantityPerSku)
				{
					_logger.LogWarning("Quantity limit exceeded for SKU {SkuId}. Setting to max {Max}",
						item.SkuId, CartConstants.MaxQuantityPerSku);
					totalQuantity = CartConstants.MaxQuantityPerSku;
				}
				validatedItems[item.SkuId] = existingItem with { Quantity = totalQuantity };
			}
			else
			{
				validatedItems[item.SkuId] = new MergeCartItemDto(item.ProductId, item.SkuId, adjustedQuantity);
			}
		}

		return validatedItems;
	}
}
