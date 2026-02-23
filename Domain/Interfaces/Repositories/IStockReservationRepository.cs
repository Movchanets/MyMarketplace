using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for stock reservation operations.
/// NOTE: Repository should NOT contain domain logic - only data access.
/// Domain operations (Cancel, ConvertToOrder) should be called from Application/Domain services.
/// </summary>
public interface IStockReservationRepository
{
	/// <summary>
	/// Gets a reservation by its ID (untracked, for read-only scenarios)
	/// </summary>
	Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a reservation by its ID (tracked, for update scenarios)
	/// </summary>
	Task<StockReservation?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all active reservations for a specific SKU (untracked)
	/// </summary>
	Task<IEnumerable<StockReservation>> GetActiveBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all reservations for a specific cart (untracked, for read-only)
	/// </summary>
	Task<IEnumerable<StockReservation>> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all reservations for a specific cart (tracked, for updates)
	/// </summary>
	Task<IEnumerable<StockReservation>> GetByCartIdTrackedAsync(Guid cartId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets active reservations for a cart (tracked, for cancel/convert operations)
	/// </summary>
	Task<IEnumerable<StockReservation>> GetActiveByCartIdTrackedAsync(Guid cartId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all reservations for a specific order (untracked)
	/// </summary>
	Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all expired reservations that need cleanup (untracked, for listing)
	/// </summary>
	Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets expired reservations for cleanup (tracked, for batch processing)
	/// </summary>
	Task<IEnumerable<StockReservation>> GetExpiredReservationsTrackedAsync(int limit = 100, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all active reservations that will expire within a specified time window
	/// </summary>
	Task<IEnumerable<StockReservation>> GetReservationsExpiringSoonAsync(TimeSpan window, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new reservation
	/// </summary>
	void Add(StockReservation reservation);

	/// <summary>
	/// Updates an existing reservation (entity must be tracked)
	/// </summary>
	void Update(StockReservation reservation);

	/// <summary>
	/// Deletes a reservation
	/// </summary>
	void Delete(StockReservation reservation);

	/// <summary>
	/// Gets total reserved quantity for a SKU
	/// </summary>
	Task<int> GetTotalReservedQuantityAsync(Guid skuId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all active reservations with pagination
	/// </summary>
	Task<(IEnumerable<StockReservation> Items, int TotalCount)> GetActiveReservationsAsync(
		int pageNumber,
		int pageSize,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets reservations by session ID (for guest carts)
	/// </summary>
	Task<IEnumerable<StockReservation>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
}
