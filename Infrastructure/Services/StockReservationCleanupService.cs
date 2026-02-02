using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for cleaning up expired stock reservations
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

		var expiredReservations = await _reservationRepository.GetExpiredReservationsAsync(cancellationToken);
		var reservationsToProcess = expiredReservations.Take(batchSize).ToList();

		if (!reservationsToProcess.Any())
		{
			_logger.LogInformation("No expired reservations found for cleanup");
			return 0;
		}

		int releasedCount = 0;

		foreach (var reservation in reservationsToProcess)
		{
			try
			{
				await ReleaseReservationInternalAsync(reservation, "Expired", cancellationToken);
				releasedCount++;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error releasing expired reservation {ReservationId} for SKU {SkuId}",
					reservation.Id, reservation.SkuId);
			}
		}

		_logger.LogInformation("Released {ReleasedCount} expired reservations out of {TotalCount}",
			releasedCount, reservationsToProcess.Count);

		return releasedCount;
	}

	/// <inheritdoc />
	public async Task<bool> ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Releasing reservation {ReservationId}", reservationId);

		var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
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

		await ReleaseReservationInternalAsync(reservation, "Manual release", cancellationToken);
		return true;
	}

	/// <inheritdoc />
	public async Task<int> ReleaseReservationsForCartAsync(Guid cartId, string? reason = null, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Releasing reservations for cart {CartId}", cartId);

		var reservations = await _reservationRepository.GetByCartIdAsync(cartId, cancellationToken);
		var activeReservations = reservations.Where(r => r.Status.IsHoldingStock()).ToList();

		int releasedCount = 0;
		foreach (var reservation in activeReservations)
		{
			try
			{
				await ReleaseReservationInternalAsync(reservation, reason ?? "Cart cleared", cancellationToken);
				releasedCount++;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error releasing reservation {ReservationId} for cart {CartId}",
					reservation.Id, cartId);
			}
		}

		_logger.LogInformation("Released {ReleasedCount} reservations for cart {CartId}", releasedCount, cartId);
		return releasedCount;
	}

	/// <inheritdoc />
	public async Task<bool> ExtendReservationAsync(Guid reservationId, int additionalMinutes, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Extending reservation {ReservationId} by {AdditionalMinutes} minutes",
			reservationId, additionalMinutes);

		var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
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

		reservation.ExtendExpiry(additionalMinutes);
		_reservationRepository.Update(reservation);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Extended reservation {ReservationId}, new expiry: {ExpiresAt}",
			reservationId, reservation.ExpiresAt);

		return true;
	}

	/// <inheritdoc />
	public async Task<ReservationStatistics> GetReservationStatisticsAsync(CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Generating reservation statistics");

		// This is a simplified implementation. In production, consider caching these statistics
		// or using raw SQL for better performance with large datasets.

		var allReservations = await GetAllReservationsAsync(cancellationToken);
		var activeReservations = allReservations.Where(r => r.Status.IsHoldingStock()).ToList();
		var expiredReservations = allReservations.Where(r => r.Status == ReservationStatus.Active && r.IsExpired()).ToList();

		var expiringSoon = activeReservations
			.Where(r => r.GetTimeRemaining() <= TimeSpan.FromHours(1))
			.ToList();

		var reservationsByStatus = allReservations
			.GroupBy(r => r.Status.ToString())
			.ToDictionary(g => g.Key, g => g.Count());

		var statistics = new ReservationStatistics
		{
			TotalActiveReservations = activeReservations.Count,
			TotalReservedQuantity = activeReservations.Sum(r => r.Quantity),
			ExpiringWithinHour = expiringSoon.Count,
			ExpiredPendingCleanup = expiredReservations.Count,
			ReservationsByStatus = reservationsByStatus,
			GeneratedAt = DateTime.UtcNow
		};

		_logger.LogDebug("Reservation statistics generated: {Active} active, {ExpiringSoon} expiring within hour",
			statistics.TotalActiveReservations, statistics.ExpiringWithinHour);

		return statistics;
	}

	private async Task ReleaseReservationInternalAsync(StockReservation reservation, string reason, CancellationToken cancellationToken)
	{
		var sku = await _skuRepository.GetByIdAsync(reservation.SkuId);
		if (sku == null)
		{
			_logger.LogWarning("SKU {SkuId} not found for reservation {ReservationId}",
				reservation.SkuId, reservation.Id);
			// Still mark reservation as expired to prevent it from being processed again
			reservation.MarkAsExpired();
			_reservationRepository.Update(reservation);
			await _unitOfWork.SaveChangesAsync(cancellationToken);
			return;
		}

		sku.ReleaseReservation(reservation);
		_skuRepository.Update(sku);
		_reservationRepository.Update(reservation);

		await _unitOfWork.SaveChangesAsync(cancellationToken);

		_logger.LogInformation(
			"Released reservation {ReservationId} for SKU {SkuCode}. Quantity: {Quantity}, Reason: {Reason}",
			reservation.Id, sku.SkuCode, reservation.Quantity, reason);
	}

	private async Task<IEnumerable<StockReservation>> GetAllReservationsAsync(CancellationToken cancellationToken)
	{
		// This is a placeholder - in production, you might want to add a method to the repository
		// to get all reservations or use a more efficient query
		var pageNumber = 1;
		var pageSize = 1000;
		var allReservations = new List<StockReservation>();

		while (true)
		{
			var (items, totalCount) = await _reservationRepository.GetActiveReservationsAsync(
				pageNumber, pageSize, cancellationToken);

			allReservations.AddRange(items);

			if (items.Count() < pageSize || allReservations.Count >= totalCount)
			{
				break;
			}

			pageNumber++;
		}

		return allReservations;
	}
}
