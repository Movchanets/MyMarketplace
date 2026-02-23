using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for cleaning up expired stock reservations.
/// All domain operations are called on tracked entities within transactions.
/// </summary>
public class StockReservationCleanupService : IStockReservationCleanupService
{
	private readonly IStockReservationRepository _reservationRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<StockReservationCleanupService> _logger;

	public StockReservationCleanupService(
		IStockReservationRepository reservationRepository,
		ISkuRepository skuRepository,
		IUnitOfWork unitOfWork,
		ILogger<StockReservationCleanupService> logger)
	{
		_reservationRepository = reservationRepository;
		_skuRepository = skuRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task<int> CleanupExpiredReservationsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Starting cleanup of expired stock reservations with batch size {BatchSize}", batchSize);

		// Get tracked entities directly for update
		var expiredReservations = await _reservationRepository.GetExpiredReservationsTrackedAsync(batchSize, cancellationToken);
		var reservationsToProcess = expiredReservations.ToList();

		if (!reservationsToProcess.Any())
		{
			_logger.LogInformation("No expired reservations found for cleanup");
			return 0;
		}

		int releasedCount = 0;

		await _unitOfWork.ExecuteInTransactionAsync(async ct =>
		{
			foreach (var reservation in reservationsToProcess)
			{
				try
				{
					var sku = await _skuRepository.GetByIdAsync(reservation.SkuId);
					if (sku == null)
					{
						_logger.LogWarning("SKU {SkuId} not found for reservation {ReservationId}",
							reservation.SkuId, reservation.Id);
						// Mark as expired even if SKU is missing
						reservation.MarkAsExpired();
						continue;
					}

					// Use domain method on SKU which handles both entities
					sku.ReleaseReservation(reservation);
					// Note: No need to call Update() - entity is tracked and EF Core detects changes automatically

					_logger.LogDebug(
						"Released expired reservation {ReservationId} for SKU {SkuCode}. Quantity: {Quantity}",
						reservation.Id, sku.SkuCode, reservation.Quantity);

					releasedCount++;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error releasing expired reservation {ReservationId} for SKU {SkuId}",
						reservation.Id, reservation.SkuId);
				}
			}

			return true;
		}, cancellationToken);

		_logger.LogInformation("Released {ReleasedCount} expired reservations out of {TotalCount}",
			releasedCount, reservationsToProcess.Count);

		return releasedCount;
	}

	/// <inheritdoc />
	public async Task<bool> ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Releasing reservation {ReservationId}", reservationId);

		// Use tracked entity for updates
		var reservation = await _reservationRepository.GetByIdTrackedAsync(reservationId, cancellationToken);
		if (reservation == null)
		{
			_logger.LogWarning("Reservation {ReservationId} not found", reservationId);
			return false;
		}

		if (!reservation.Status.IsHoldingStock())
		{
			_logger.LogWarning("Reservation {ReservationId} is not active (status: {Status})",
				reservationId, reservation.Status);
			return false;
		}

		return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
		{
			var sku = await _skuRepository.GetByIdAsync(reservation.SkuId);
			if (sku == null)
			{
				_logger.LogWarning("SKU {SkuId} not found for reservation {ReservationId}",
					reservation.SkuId, reservationId);
				reservation.MarkAsExpired();
				await _unitOfWork.SaveChangesAsync(ct);
				return false;
			}

			// Domain method handles both reservation status and SKU reserved quantity
			sku.ReleaseReservation(reservation);
			// Note: No need to call Update() - entity is tracked and EF Core detects changes automatically

			_logger.LogInformation(
				"Released reservation {ReservationId} for SKU {SkuCode}. Quantity: {Quantity}",
				reservation.Id, sku.SkuCode, reservation.Quantity);

			return true;
		}, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<int> ReleaseReservationsForCartAsync(Guid cartId, string? reason = null, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Releasing reservations for cart {CartId}", cartId);

		// Get tracked active reservations for this cart
		var activeReservations = (await _reservationRepository.GetActiveByCartIdTrackedAsync(cartId, cancellationToken)).ToList();

		if (!activeReservations.Any())
		{
			_logger.LogInformation("No active reservations found for cart {CartId}", cartId);
			return 0;
		}

		int releasedCount = 0;

		await _unitOfWork.ExecuteInTransactionAsync(async ct =>
		{
			foreach (var reservation in activeReservations)
			{
				try
				{
					var sku = await _skuRepository.GetByIdAsync(reservation.SkuId);
					if (sku == null)
					{
						_logger.LogWarning("SKU {SkuId} not found for reservation {ReservationId}",
							reservation.SkuId, reservation.Id);
						reservation.Cancel(reason ?? "SKU not found");
						continue;
					}

					sku.ReleaseReservation(reservation);
					// Note: No need to call Update() - entity is tracked and EF Core detects changes automatically
					releasedCount++;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error releasing reservation {ReservationId} for cart {CartId}",
						reservation.Id, cartId);
				}
			}

			return true;
		}, cancellationToken);

		_logger.LogInformation("Released {ReleasedCount} reservations for cart {CartId}", releasedCount, cartId);
		return releasedCount;
	}

	/// <inheritdoc />
	public async Task<bool> ExtendReservationAsync(Guid reservationId, int additionalMinutes, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Extending reservation {ReservationId} by {AdditionalMinutes} minutes",
			reservationId, additionalMinutes);

		// Use tracked entity for updates
		var reservation = await _reservationRepository.GetByIdTrackedAsync(reservationId, cancellationToken);
		if (reservation == null)
		{
			_logger.LogWarning("Reservation {ReservationId} not found", reservationId);
			return false;
		}

		if (reservation.Status != ReservationStatus.Active)
		{
			_logger.LogWarning("Cannot extend reservation {ReservationId} with status {Status}",
				reservationId, reservation.Status);
			return false;
		}

		// Call domain method on tracked entity
		reservation.ExtendExpiry(additionalMinutes);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Extended reservation {ReservationId}, new expiry: {ExpiresAt}",
			reservationId, reservation.ExpiresAt);

		return true;
	}

	/// <inheritdoc />
	public async Task<ReservationStatistics> GetReservationStatisticsAsync(CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Generating reservation statistics");

		var (activeItems, totalActive) = await _reservationRepository.GetActiveReservationsAsync(1, 10000, cancellationToken);
		var activeReservations = activeItems.ToList();

		var expiredPending = (await _reservationRepository.GetExpiredReservationsAsync(cancellationToken)).Count();

		var expiringSoon = activeReservations
			.Where(r => r.GetTimeRemaining() <= TimeSpan.FromHours(1))
			.Count();

		var statistics = new ReservationStatistics
		{
			TotalActiveReservations = totalActive,
			TotalReservedQuantity = activeReservations.Sum(r => r.Quantity),
			ExpiringWithinHour = expiringSoon,
			ExpiredPendingCleanup = expiredPending,
			ReservationsByStatus = new Dictionary<string, int>
			{
				[ReservationStatus.Active.ToString()] = totalActive,
				[ReservationStatus.Expired.ToString()] = expiredPending
			},
			GeneratedAt = DateTime.UtcNow
		};

		_logger.LogDebug("Reservation statistics generated: {Active} active, {ExpiringSoon} expiring within hour",
			statistics.TotalActiveReservations, statistics.ExpiringWithinHour);

		return statistics;
	}
}
