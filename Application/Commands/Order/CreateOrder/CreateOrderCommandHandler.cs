using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace Application.Commands.Order.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ServiceResponse<OrderDto>>
{
	private readonly IOrderRepository _orderRepository;
	private readonly ICartRepository _cartRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUserRepository _userRepository;
	private readonly IStockReservationRepository _stockReservationRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<CreateOrderCommandHandler> _logger;

	public CreateOrderCommandHandler(
		IOrderRepository orderRepository,
		ICartRepository cartRepository,
		ISkuRepository skuRepository,
		IUserRepository userRepository,
		IStockReservationRepository stockReservationRepository,
		IUnitOfWork unitOfWork,
		ILogger<CreateOrderCommandHandler> logger)
	{
		_orderRepository = orderRepository;
		_cartRepository = cartRepository;
		_skuRepository = skuRepository;
		_userRepository = userRepository;
		_stockReservationRepository = stockReservationRepository;
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

		// Use RepeatableRead to prevent race conditions on stock deduction between validation and commit.
		// Under default ReadCommitted, concurrent orders could both pass validation and oversell.
		await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

		try
		{
			// Validate user
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<OrderDto>(false, "User not found", null);
			}

			// FIX: Load cart WITH Product and SKU navigation properties to avoid null references.
			// Previously used GetByUserIdAsync which only loaded CartItems without Product/SKU,
			// causing silent item skips when cartItem.Product was null.
			var cart = await _cartRepository.GetByUserIdWithProductsAsync(domainUser.Id, cancellationToken);
			if (cart is null || !cart.Items.Any())
			{
				_logger.LogWarning("Cart is empty for user {UserId}", request.UserId);
				return new ServiceResponse<OrderDto>(false, "Cart is empty", null);
			}

			// Pre-fetch all SKUs and active cart reservations in a single pass.
			var skuMap = new Dictionary<Guid, SkuEntity>();
			var activeCartReservations = (await _stockReservationRepository
				.GetActiveByCartIdTrackedAsync(cart.Id, cancellationToken)).ToList();

			foreach (var cartItem in cart.Items)
			{
				var sku = await _skuRepository.GetByIdAsync(cartItem.SkuId);
				if (sku is null)
				{
					return new ServiceResponse<OrderDto>(false,
						$"Product variant not found for item in cart (SKU ID: {cartItem.SkuId})", null);
				}

				if (cartItem.Product is null)
				{
					_logger.LogError("Product {ProductId} not loaded for cart item {CartItemId}. " +
						"This indicates a data integrity issue.", cartItem.ProductId, cartItem.Id);
					return new ServiceResponse<OrderDto>(false,
						$"Product not found for cart item (Product ID: {cartItem.ProductId})", null);
				}

				// Calculate how much is already reserved for this SKU by this cart
				var skuReservations = activeCartReservations
					.Where(r => r.SkuId == cartItem.SkuId)
					.ToList();
				var reservedForThisSku = skuReservations.Sum(r => r.Quantity);

				// The unreserved portion must be available from the unreserved stock pool
				var unreservedNeeded = cartItem.Quantity - reservedForThisSku;
				if (unreservedNeeded > 0 && sku.AvailableQuantity < unreservedNeeded)
				{
					return new ServiceResponse<OrderDto>(false,
						$"Insufficient stock for '{sku.SkuCode}'. Available: {sku.AvailableQuantity + reservedForThisSku}, Requested: {cartItem.Quantity}.", null);
				}

				skuMap[cartItem.SkuId] = sku;
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

			// Create order items — use existing reservations when possible, deduct remainder directly
			foreach (var cartItem in cart.Items)
			{
				var sku = skuMap[cartItem.SkuId];
				var product = cartItem.Product!; // Already validated above

				// Find active reservations for this SKU from this cart
				var skuReservations = activeCartReservations
					.Where(r => r.SkuId == cartItem.SkuId)
					.ToList();

				var quantityFromReservations = 0;

				// Convert existing reservations to order deductions
				foreach (var reservation in skuReservations)
				{
					sku.ConvertReservationToDeduction(reservation, order.Id);
					quantityFromReservations += reservation.Quantity;

					_logger.LogDebug(
						"Converted reservation {ReservationId} for SKU {SkuCode}, quantity: {Qty}",
						reservation.Id, sku.SkuCode, reservation.Quantity);
				}

				// Deduct any remaining quantity not covered by reservations
				var remainingQuantity = cartItem.Quantity - quantityFromReservations;
				if (remainingQuantity > 0)
				{
					sku.DeductStock(remainingQuantity);

					_logger.LogDebug(
						"Direct stock deduction for SKU {SkuCode}, quantity: {Qty} (no reservation)",
						sku.SkuCode, remainingQuantity);
				}

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

			_logger.LogInformation("Order {OrderNumber} created successfully for user {UserId} with {ItemCount} items",
				order.OrderNumber, request.UserId, order.Items.Count);

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
