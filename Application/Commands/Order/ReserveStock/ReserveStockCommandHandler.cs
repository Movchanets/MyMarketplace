using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Commands.Order.ReserveStock;

/// <summary>
/// Handles stock reservation when a user proceeds to checkout.
/// Reserves all cart items for a limited time to prevent overselling.
/// If existing active reservations exist for the cart, they are reused/extended.
/// </summary>
public sealed class ReserveStockCommandHandler
	: IRequestHandler<ReserveStockCommand, ServiceResponse<StockReservationResultDto>>
{
	private readonly ICartRepository _cartRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUserRepository _userRepository;
	private readonly IStockReservationRepository _stockReservationRepository;
	private readonly IStockReservationCleanupService _cleanupService;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<ReserveStockCommandHandler> _logger;

	public ReserveStockCommandHandler(
		ICartRepository cartRepository,
		ISkuRepository skuRepository,
		IUserRepository userRepository,
		IStockReservationRepository stockReservationRepository,
		IStockReservationCleanupService cleanupService,
		IUnitOfWork unitOfWork,
		ILogger<ReserveStockCommandHandler> logger)
	{
		_cartRepository = cartRepository;
		_skuRepository = skuRepository;
		_userRepository = userRepository;
		_stockReservationRepository = stockReservationRepository;
		_cleanupService = cleanupService;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<StockReservationResultDto>> Handle(
		ReserveStockCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Reserving stock for user {UserId} at checkout", request.UserId);

		// Validate user
		var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
		if (domainUser is null)
		{
			_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
			return new ServiceResponse<StockReservationResultDto>(false, "User not found", null);
		}

		// Load cart with products
		var cart = await _cartRepository.GetByUserIdWithProductsAsync(domainUser.Id, cancellationToken);
		if (cart is null || !cart.Items.Any())
		{
			_logger.LogWarning("Cart is empty for user {UserId}", request.UserId);
			return new ServiceResponse<StockReservationResultDto>(false, "Cart is empty", null);
		}

		// Release any existing reservations for this cart before creating new ones
		var existingReservations = (await _stockReservationRepository
			.GetActiveByCartIdTrackedAsync(cart.Id, cancellationToken)).ToList();

		if (existingReservations.Any())
		{
			_logger.LogInformation(
				"Releasing {Count} existing reservations for cart {CartId} before re-reserving",
				existingReservations.Count, cart.Id);

			await _cleanupService.ReleaseReservationsForCartAsync(
				cart.Id, "Re-reserving for new checkout attempt", cancellationToken);
		}

		// Use RepeatableRead to prevent concurrent reservation race conditions
		await using var transaction = await _unitOfWork.BeginTransactionAsync(
			IsolationLevel.RepeatableRead, cancellationToken);

		try
		{
			var reservedItems = new List<ReservedItemDto>();
			DateTime earliestExpiry = DateTime.MaxValue;

			foreach (var cartItem in cart.Items)
			{
				var sku = await _skuRepository.GetByIdAsync(cartItem.SkuId);
				if (sku is null)
				{
					return new ServiceResponse<StockReservationResultDto>(false,
						$"Product variant not found (SKU ID: {cartItem.SkuId})", null);
				}

				// Check available stock (accounts for other users' reservations)
				if (!sku.CanReserve(cartItem.Quantity))
				{
					return new ServiceResponse<StockReservationResultDto>(false,
						$"Insufficient stock for '{sku.SkuCode}'. Available: {sku.AvailableQuantity}, Requested: {cartItem.Quantity}.",
						null);
				}

				// Create reservation via domain method
				var reservation = sku.ReserveStock(
					cartItem.Quantity,
					cart.Id,
					request.SessionId,
					request.IpAddress,
					request.UserAgent);

				_stockReservationRepository.Add(reservation);
				_skuRepository.Update(sku);

				reservedItems.Add(new ReservedItemDto(
					reservation.Id,
					sku.Id,
					sku.SkuCode,
					cartItem.Quantity,
					reservation.ExpiresAt));

				if (reservation.ExpiresAt < earliestExpiry)
					earliestExpiry = reservation.ExpiresAt;
			}

			await _unitOfWork.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			_logger.LogInformation(
				"Reserved stock for {ItemCount} items in cart {CartId}. Expires at {ExpiresAt}",
				reservedItems.Count, cart.Id, earliestExpiry);

			var result = new StockReservationResultDto(
				cart.Id,
				reservedItems,
				earliestExpiry,
				reservedItems.Sum(r => r.Quantity));

			return new ServiceResponse<StockReservationResultDto>(
				true, "Stock reserved successfully", result);
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync(cancellationToken);
			_logger.LogError(ex, "Error reserving stock for user {UserId}", request.UserId);
			return new ServiceResponse<StockReservationResultDto>(
				false, "An error occurred while reserving stock", null);
		}
	}
}
