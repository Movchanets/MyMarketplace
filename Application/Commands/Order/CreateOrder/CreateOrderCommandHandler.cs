using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Commands.Order.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ServiceResponse<OrderDto>>
{
	private readonly IOrderRepository _orderRepository;
	private readonly ICartRepository _cartRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<CreateOrderCommandHandler> _logger;

	public CreateOrderCommandHandler(
		IOrderRepository orderRepository,
		ICartRepository cartRepository,
		ISkuRepository skuRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<CreateOrderCommandHandler> logger)
	{
		_orderRepository = orderRepository;
		_cartRepository = cartRepository;
		_skuRepository = skuRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Creating order for user {UserId}", request.UserId);

		// Check idempotency key if provided
		if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
		{
			var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
			if (existingOrder != null)
			{
				_logger.LogInformation("Order with idempotency key {IdempotencyKey} already exists: {OrderId}",
					request.IdempotencyKey, existingOrder.Id);
				var existingDto = MapToOrderDto(existingOrder);
				return new ServiceResponse<OrderDto>(true, "Order already exists", existingDto);
			}
		}

		await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

		try
		{
			// Validate user
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<OrderDto>(false, "User not found", null);
			}

			// Get cart
			var cart = await _cartRepository.GetByUserIdAsync(domainUser.Id, cancellationToken);
			if (cart is null || !cart.Items.Any())
			{
				_logger.LogWarning("Cart is empty for user {UserId}", request.UserId);
				return new ServiceResponse<OrderDto>(false, "Cart is empty", null);
			}

			// Validate inventory for all items
			foreach (var cartItem in cart.Items)
			{
				var sku = await _skuRepository.GetByIdAsync(cartItem.SkuId);
				if (sku is null)
				{
					return new ServiceResponse<OrderDto>(false, $"Product variant not found for item in cart", null);
				}

				if (sku.StockQuantity < cartItem.Quantity)
				{
					return new ServiceResponse<OrderDto>(false,
						$"Insufficient stock for '{sku.SkuCode}'. Only {sku.StockQuantity} items available.", null);
				}
			}

			// Create order
			var order = new Domain.Entities.Order(
				domainUser.Id,
				request.ShippingAddress,
				request.DeliveryMethod,
				request.PaymentMethod,
				request.IdempotencyKey);

			// Set customer notes if provided
			if (!string.IsNullOrWhiteSpace(request.CustomerNotes))
			{
				order.SetCustomerNotes(request.CustomerNotes);
			}

			// Create order items from cart items
			foreach (var cartItem in cart.Items)
			{
				var sku = await _skuRepository.GetByIdAsync(cartItem.SkuId);
				var product = cartItem.Product;

				if (sku is null || product is null)
				{
					continue; // Skip invalid items
				}

				// Deduct inventory
				sku.DeductStock(cartItem.Quantity);
				_skuRepository.Update(sku);

				// Create order item with historical data
				var orderItem = new OrderItem(
					order.Id,
					cartItem.ProductId,
					cartItem.SkuId,
					cartItem.Quantity,
					sku.Price,
					product.Name,
					product.BaseImageUrl,
					sku.SkuCode,
					sku.Attributes?.RootElement.ToString());

				order.AddItem(orderItem);
			}

			// Apply promo code if provided (simplified - in real app, validate promo code)
			if (!string.IsNullOrWhiteSpace(request.PromoCode))
			{
				// TODO: Implement promo code validation and discount calculation
				// For now, just store the promo code
			}

			// Set shipping cost (simplified - in real app, calculate based on delivery method)
			order.SetShippingCost(0); // Free shipping for now

			// Save order
			_orderRepository.Add(order);

			// Clear cart atomically (EF Core change tracking automatically detects modifications)
			cart.Clear();

			await _unitOfWork.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			_logger.LogInformation("Order {OrderNumber} created successfully for user {UserId}",
				order.OrderNumber, request.UserId);

			var orderDto = MapToOrderDto(order);
			return new ServiceResponse<OrderDto>(true, $"Order {order.OrderNumber} created successfully", orderDto);
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync(cancellationToken);
			_logger.LogError(ex, "Error creating order for user {UserId}", request.UserId);
			return new ServiceResponse<OrderDto>(false, "An error occurred while creating order", null);
		}
	}

	private static OrderDto MapToOrderDto(Domain.Entities.Order order)
	{
		var items = order.Items.Select(item => new OrderItemDto(
			item.Id,
			item.ProductId,
			item.ProductNameSnapshot,
			item.ProductImageUrlSnapshot,
			item.SkuId,
			item.SkuCodeSnapshot,
			item.SkuAttributesSnapshot,
			item.Quantity,
			item.PriceAtPurchase,
			item.GetSubtotal()
		)).ToList();

		return new OrderDto(
			order.Id,
			order.OrderNumber,
			order.UserId,
			items,
			order.TotalPrice,
			order.Status,
			order.PaymentStatus,
			order.DeliveryMethod,
			order.PaymentMethod,
			order.CreatedAt,
			order.TrackingNumber,
			order.ShippingCarrier
		);
	}
}
