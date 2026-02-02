using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for Order aggregate with filtering, pagination, and ACID support
/// </summary>
public class OrderRepository : IOrderRepository
{
	private readonly AppDbContext _db;

	public OrderRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		return await _db.Orders
			.Include(o => o.Items)
			.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
	}

	public async Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
	{
		return await _db.Orders
			.Include(o => o.Items)
				.ThenInclude(i => i.Product)
			.Include(o => o.Items)
				.ThenInclude(i => i.Sku)
			.AsSplitQuery()
			.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
	}

	public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
	{
		return await _db.Orders
			.Include(o => o.Items)
			.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
	}

	public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetByUserIdAsync(
		Guid userId,
		OrderStatus? status = null,
		DateTime? fromDate = null,
		DateTime? toDate = null,
		string? sortBy = null,
		bool sortDescending = true,
		int pageNumber = 1,
		int pageSize = 20,
		CancellationToken cancellationToken = default)
	{
		var query = _db.Orders
			.Include(o => o.Items)
			.Where(o => o.UserId == userId);

		// Apply filters
		if (status.HasValue)
		{
			query = query.Where(o => o.Status == status.Value);
		}

		if (fromDate.HasValue)
		{
			query = query.Where(o => o.CreatedAt >= fromDate.Value);
		}

		if (toDate.HasValue)
		{
			query = query.Where(o => o.CreatedAt <= toDate.Value);
		}

		// Get total count before pagination
		var totalCount = await query.CountAsync(cancellationToken);

		// Apply sorting
		query = ApplySorting(query, sortBy, sortDescending);

		// Apply pagination
		var orders = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return (orders, totalCount);
	}

	public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetAllAsync(
		OrderStatus? status = null,
		PaymentStatus? paymentStatus = null,
		DateTime? fromDate = null,
		DateTime? toDate = null,
		string? sortBy = null,
		bool sortDescending = true,
		int pageNumber = 1,
		int pageSize = 20,
		CancellationToken cancellationToken = default)
	{
		var query = _db.Orders
			.Include(o => o.Items)
			.AsQueryable();

		// Apply filters
		if (status.HasValue)
		{
			query = query.Where(o => o.Status == status.Value);
		}

		if (paymentStatus.HasValue)
		{
			query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
		}

		if (fromDate.HasValue)
		{
			query = query.Where(o => o.CreatedAt >= fromDate.Value);
		}

		if (toDate.HasValue)
		{
			query = query.Where(o => o.CreatedAt <= toDate.Value);
		}

		// Get total count before pagination
		var totalCount = await query.CountAsync(cancellationToken);

		// Apply sorting
		query = ApplySorting(query, sortBy, sortDescending);

		// Apply pagination
		var orders = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return (orders, totalCount);
	}

	public async Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
	{
		return await _db.Orders.AnyAsync(o => o.IdempotencyKey == idempotencyKey, cancellationToken);
	}

	public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
	{
		return await _db.Orders
			.Include(o => o.Items)
			.FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, cancellationToken);
	}

	public async Task<OrderStatistics> GetUserOrderStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		// Use projection for better performance instead of loading all orders
		var stats = await _db.Orders
			.Where(o => o.UserId == userId)
			.GroupBy(_ => 1)
			.Select(g => new OrderStatistics
			{
				TotalOrders = g.Count(),
				PendingOrders = g.Count(o => o.Status == OrderStatus.Pending),
				ConfirmedOrders = g.Count(o => o.Status == OrderStatus.Confirmed),
				ProcessingOrders = g.Count(o => o.Status == OrderStatus.Processing),
				ShippedOrders = g.Count(o => o.Status == OrderStatus.Shipped),
				DeliveredOrders = g.Count(o => o.Status == OrderStatus.Delivered),
				CancelledOrders = g.Count(o => o.Status == OrderStatus.Cancelled),
				TotalSpent = g.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalPrice)
			})
			.FirstOrDefaultAsync(cancellationToken);

		return stats ?? new OrderStatistics();
	}

	public void Add(Order order)
	{
		_db.Orders.Add(order);
	}

	public void Update(Order order)
	{
		_db.Orders.Update(order);
	}

	public async Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		return await _db.Orders.CountAsync(o => o.UserId == userId, cancellationToken);
	}

	public async Task<IEnumerable<Order>> GetPendingOrdersAsync(
		TimeSpan olderThan,
		int limit = 100,
		CancellationToken cancellationToken = default)
	{
		var cutoffTime = DateTime.UtcNow - olderThan;

		return await _db.Orders
			.Include(o => o.Items)
			.Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < cutoffTime)
			.OrderBy(o => o.CreatedAt)
			.Take(limit)
			.ToListAsync(cancellationToken);
	}

	private static IQueryable<Order> ApplySorting(IQueryable<Order> query, string? sortBy, bool sortDescending)
	{
		return sortBy?.ToLowerInvariant() switch
		{
			"totalprice" => sortDescending
				? query.OrderByDescending(o => o.TotalPrice)
				: query.OrderBy(o => o.TotalPrice),
			"status" => sortDescending
				? query.OrderByDescending(o => o.Status)
				: query.OrderBy(o => o.Status),
			"ordernumber" => sortDescending
				? query.OrderByDescending(o => o.OrderNumber)
				: query.OrderBy(o => o.OrderNumber),
			_ => sortDescending
				? query.OrderByDescending(o => o.CreatedAt)
				: query.OrderBy(o => o.CreatedAt)
		};
	}
}
