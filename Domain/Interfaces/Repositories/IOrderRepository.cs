using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for Order aggregate with async CRUD operations and ACID support
/// </summary>
public interface IOrderRepository
{
	/// <summary>
	/// Gets an order by its ID with all items
	/// </summary>
	Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an order by its ID with items, products and SKU details loaded
	/// </summary>
	Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an order by its order number
	/// </summary>
	Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets orders for a specific user with filtering, sorting, and pagination
	/// </summary>
	Task<(IEnumerable<Order> Orders, int TotalCount)> GetByUserIdAsync(
		Guid userId,
		OrderStatus? status = null,
		DateTime? fromDate = null,
		DateTime? toDate = null,
		string? sortBy = null,
		bool sortDescending = true,
		int pageNumber = 1,
		int pageSize = 20,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all orders with filtering, sorting, and pagination (for admin)
	/// </summary>
	Task<(IEnumerable<Order> Orders, int TotalCount)> GetAllAsync(
		OrderStatus? status = null,
		PaymentStatus? paymentStatus = null,
		DateTime? fromDate = null,
		DateTime? toDate = null,
		string? sortBy = null,
		bool sortDescending = true,
		int pageNumber = 1,
		int pageSize = 20,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if an order exists with the given idempotency key
	/// </summary>
	Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an order by idempotency key
	/// </summary>
	Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets order statistics for a user
	/// </summary>
	Task<OrderStatistics> GetUserOrderStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new order
	/// </summary>
	void Add(Order order);

	/// <summary>
	/// Updates an existing order
	/// </summary>
	void Update(Order order);

	/// <summary>
	/// Gets the count of orders for a specific user
	/// </summary>
	Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets orders that need processing (e.g., for background jobs)
	/// </summary>
	Task<IEnumerable<Order>> GetPendingOrdersAsync(
		TimeSpan olderThan,
		int limit = 100,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Order statistics for a user
/// </summary>
public class OrderStatistics
{
	public int TotalOrders { get; set; }
	public int PendingOrders { get; set; }
	public int ConfirmedOrders { get; set; }
	public int ProcessingOrders { get; set; }
	public int ShippedOrders { get; set; }
	public int DeliveredOrders { get; set; }
	public int CancelledOrders { get; set; }
	public decimal TotalSpent { get; set; }
}
