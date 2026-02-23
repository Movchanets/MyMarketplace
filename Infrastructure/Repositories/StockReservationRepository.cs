using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for stock reservation operations
/// </summary>
public class StockReservationRepository : IStockReservationRepository
{
	private readonly AppDbContext _context;

	public StockReservationRepository(AppDbContext context)
	{
		_context = context;
	}

	/// <inheritdoc />
	public async Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.AsNoTracking()
			.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
	}

	/// <summary>
	/// Gets a tracked reservation instance for update scenarios
	/// </summary>
	public async Task<StockReservation?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IEnumerable<StockReservation>> GetActiveBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.AsNoTracking()
			.Where(r => r.SkuId == skuId && r.Status == ReservationStatus.Active)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IEnumerable<StockReservation>> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.AsNoTracking()
			.Where(r => r.CartId == cartId)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// Gets tracked reservations for a cart (for update scenarios)
	/// </summary>
	public async Task<IEnumerable<StockReservation>> GetByCartIdTrackedAsync(Guid cartId, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.Where(r => r.CartId == cartId)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.AsNoTracking()
			.Where(r => r.OrderId == orderId)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;
		return await _context.StockReservations
			.AsNoTracking()
			.Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt < now)
			.OrderBy(r => r.ExpiresAt)
			.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// Gets tracked expired reservations for cleanup (update scenarios)
	/// </summary>
	public async Task<IEnumerable<StockReservation>> GetExpiredReservationsTrackedAsync(int limit = 100, CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;
		return await _context.StockReservations
			.Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt < now)
			.OrderBy(r => r.ExpiresAt)
			.Take(limit)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IEnumerable<StockReservation>> GetReservationsExpiringSoonAsync(TimeSpan window, CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;
		var threshold = now.Add(window);
		return await _context.StockReservations
			.Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt >= now && r.ExpiresAt <= threshold)
			.OrderBy(r => r.ExpiresAt)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public void Add(StockReservation reservation)
	{
		_context.StockReservations.Add(reservation);
	}

	/// <inheritdoc />
	public void Update(StockReservation reservation)
	{
		_context.StockReservations.Update(reservation);
	}

	/// <inheritdoc />
	public void Delete(StockReservation reservation)
	{
		_context.StockReservations.Remove(reservation);
	}

	/// <inheritdoc />
	public async Task<int> GetTotalReservedQuantityAsync(Guid skuId, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.Where(r => r.SkuId == skuId && r.Status == ReservationStatus.Active)
			.SumAsync(r => r.Quantity, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<(IEnumerable<StockReservation> Items, int TotalCount)> GetActiveReservationsAsync(
		int pageNumber,
		int pageSize,
		CancellationToken cancellationToken = default)
	{
		var query = _context.StockReservations
			.Where(r => r.Status == ReservationStatus.Active);

		var totalCount = await query.CountAsync(cancellationToken);

		var items = await query
			.OrderByDescending(r => r.CreatedAt)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return (items, totalCount);
	}

	/// <inheritdoc />
	public async Task<IEnumerable<StockReservation>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.AsNoTracking()
			.Where(r => r.SessionId == sessionId)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IEnumerable<StockReservation>> GetActiveByCartIdTrackedAsync(Guid cartId, CancellationToken cancellationToken = default)
	{
		return await _context.StockReservations
			.Where(r => r.CartId == cartId && r.Status == ReservationStatus.Active)
			.ToListAsync(cancellationToken);
	}

}
