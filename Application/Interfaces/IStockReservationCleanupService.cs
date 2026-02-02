namespace Application.Interfaces;

/// <summary>
/// Service interface for cleaning up expired stock reservations
/// </summary>
public interface IStockReservationCleanupService
{
	/// <summary>
	/// Releases all expired reservations and returns stock to available pool
	/// </summary>
	/// <param name="batchSize">Maximum number of reservations to process in one batch</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Number of reservations released</returns>
	Task<int> CleanupExpiredReservationsAsync(int batchSize = 100, CancellationToken cancellationToken = default);

	/// <summary>
	/// Releases a specific reservation by ID
	/// </summary>
	/// <param name="reservationId">The reservation ID to release</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if successfully released, false if not found or already released</returns>
	Task<bool> ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Releases all active reservations for a specific cart
	/// </summary>
	/// <param name="cartId">The cart ID</param>
	/// <param name="reason">Optional reason for release</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Number of reservations released</returns>
	Task<int> ReleaseReservationsForCartAsync(Guid cartId, string? reason = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Extends the expiry time of an active reservation
	/// </summary>
	/// <param name="reservationId">The reservation ID</param>
	/// <param name="additionalMinutes">Minutes to extend</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if successfully extended, false if not found or not active</returns>
	Task<bool> ExtendReservationAsync(Guid reservationId, int additionalMinutes, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets statistics about current reservations
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Reservation statistics</returns>
	Task<ReservationStatistics> GetReservationStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about stock reservations
/// </summary>
public class ReservationStatistics
{
	/// <summary>
	/// Total number of active reservations
	/// </summary>
	public int TotalActiveReservations { get; set; }

	/// <summary>
	/// Total quantity reserved across all active reservations
	/// </summary>
	public int TotalReservedQuantity { get; set; }

	/// <summary>
	/// Number of reservations expiring within the next hour
	/// </summary>
	public int ExpiringWithinHour { get; set; }

	/// <summary>
	/// Number of expired reservations pending cleanup
	/// </summary>
	public int ExpiredPendingCleanup { get; set; }

	/// <summary>
	/// Number of reservations by status
	/// </summary>
	public Dictionary<string, int> ReservationsByStatus { get; set; } = new();

	/// <summary>
	/// When the statistics were generated
	/// </summary>
	public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
