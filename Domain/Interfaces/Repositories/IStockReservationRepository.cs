using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for stock reservation operations
/// </summary>
public interface IStockReservationRepository
{
	/// <summary>
	/// Gets a reservation by its ID
	/// </summary>
	Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all active reservations for a specific SKU
	/// </summary>
	Task<IEnumerable<StockReservation>> GetActiveBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all reservations for a specific cart
	/// </summary>
	Task<IEnumerable<StockReservation>> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all reservations for a specific order
	/// </summary>
	Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all expired reservations that need cleanup
	/// </summary>
	Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all active reservations that will expire within a specified time window
	/// </summary>
	Task<IEnumerable<StockReservation>> GetReservationsExpiringSoonAsync(TimeSpan window, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new reservation
	/// </summary>
	void Add(StockReservation reservation);

	/// <summary>
	/// Updates an existing reservation
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

	/// <summary>
	/// Cancels all active reservations for a cart
	/// </summary>
	Task<int> CancelReservationsForCartAsync(Guid cartId, string? reason = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Converts reservations to order deductions
	/// </summary>
	Task<bool> ConvertReservationsToOrderAsync(Guid cartId, Guid orderId, CancellationToken cancellationToken = default);
}
