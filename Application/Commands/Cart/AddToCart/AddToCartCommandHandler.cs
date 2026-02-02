using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Cart.AddToCart;

public sealed class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, ServiceResponse<CartDto>>
{
	private readonly ICartService _cartService;
	private readonly ICartRepository _cartRepository;
	private readonly IProductRepository _productRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<AddToCartCommandHandler> _logger;

	public AddToCartCommandHandler(
		ICartService cartService,
		ICartRepository cartRepository,
		IProductRepository productRepository,
		ISkuRepository skuRepository,
		IUnitOfWork unitOfWork,
		ILogger<AddToCartCommandHandler> logger)
	{
		_cartService = cartService;
		_cartRepository = cartRepository;
		_productRepository = productRepository;
		_skuRepository = skuRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<CartDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Adding product {ProductId} (SKU: {SkuId}) to cart for user {UserId}",
			request.ProductId, request.SkuId, request.UserId);

		try
		{
			// Validate quantity
			if (request.Quantity <= 0)
			{
				return new ServiceResponse<CartDto>(false, "Quantity must be greater than zero", null);
			}

			// Validate product and SKU
			var productValidation = await ValidateProductAndSkuAsync(request);
			if (!productValidation.IsValid)
			{
				return new ServiceResponse<CartDto>(false, productValidation.ErrorMessage!, null);
			}

			var sku = productValidation.Sku!;

			// Inventory validation for requested quantity
			if (sku.StockQuantity < request.Quantity)
			{
				_logger.LogWarning(
					"Insufficient inventory for SKU {SkuId}. Requested: {Requested}, Available: {Available}",
					request.SkuId, request.Quantity, sku.StockQuantity);
				return new ServiceResponse<CartDto>(false,
					$"Insufficient stock. Only {sku.StockQuantity} items available.", null);
			}

			// Execute cart operation
			var result = await AddItemToCartAsync(request, sku, cancellationToken);

			if (!result.IsSuccess)
			{
				return new ServiceResponse<CartDto>(false, result.Message, null);
			}

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error adding item to cart for user {UserId}: {ErrorMessage}", request.UserId, ex.Message);
			return new ServiceResponse<CartDto>(false, $"An error occurred while adding item to cart: {ex.Message}", null);
		}
	}

	private async Task<ServiceResponse<CartDto>> AddItemToCartAsync(
		AddToCartCommand request,
		SkuEntity sku,
		CancellationToken cancellationToken)
	{
		// Delegate to CartService which encapsulates validation and transactional logic
		var serviceResult = await _cartService.AddOrUpdateItemAsync(request.UserId, request.ProductId, request.SkuId, request.Quantity, cancellationToken);
		if (!serviceResult.IsSuccess)
		{
			return new ServiceResponse<CartDto>(false, serviceResult.Message, null);
		}

		return serviceResult;
	}

	private async Task<(bool IsValid, string? ErrorMessage, SkuEntity? Sku)> ValidateProductAndSkuAsync(
		AddToCartCommand request)
	{
		// Validate product
		var product = await _productRepository.GetByIdAsync(request.ProductId);
		if (product is null)
		{
			_logger.LogWarning("Product {ProductId} not found", request.ProductId);
			return (false, "Product not found", null);
		}

		if (!product.IsActive)
		{
			_logger.LogWarning("Product {ProductId} is not active", request.ProductId);
			return (false, "Product is not available", null);
		}

		// Validate SKU
		var sku = await _skuRepository.GetByIdAsync(request.SkuId);
		if (sku is null)
		{
			_logger.LogWarning("SKU {SkuId} not found", request.SkuId);
			return (false, "Product variant not found", null);
		}

		if (sku.ProductId != request.ProductId)
		{
			_logger.LogWarning("SKU {SkuId} does not belong to product {ProductId}",
				request.SkuId, request.ProductId);
			return (false, "Invalid product variant", null);
		}

		return (true, null, sku);
	}
}
