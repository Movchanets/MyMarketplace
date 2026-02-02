namespace Domain.Enums;

/// <summary>
/// Represents the current status of an order in the order lifecycle
/// </summary>
public enum OrderStatus
{
	/// <summary>
	/// Order has been created but not yet confirmed
	/// </summary>
	Pending = 0,

	/// <summary>
	/// Order has been confirmed and is awaiting processing
	/// </summary>
	Confirmed = 1,

	/// <summary>
	/// Order is being processed (picked, packed, prepared)
	/// </summary>
	Processing = 2,

	/// <summary>
	/// Order has been shipped and is in transit
	/// </summary>
	Shipped = 3,

	/// <summary>
	/// Order has been delivered to the customer
	/// </summary>
	Delivered = 4,

	/// <summary>
	/// Order has been cancelled
	/// </summary>
	Cancelled = 5
}

/// <summary>
/// Extension methods for OrderStatus enum
/// </summary>
public static class OrderStatusExtensions
{
	/// <summary>
	/// Gets the display name for the order status
	/// </summary>
	public static string GetDisplayName(this OrderStatus status)
	{
		return status switch
		{
			OrderStatus.Pending => "Pending",
			OrderStatus.Confirmed => "Confirmed",
			OrderStatus.Processing => "Processing",
			OrderStatus.Shipped => "Shipped",
			OrderStatus.Delivered => "Delivered",
			OrderStatus.Cancelled => "Cancelled",
			_ => "Unknown"
		};
	}

	/// <summary>
	/// Checks if the order status allows cancellation
	/// </summary>
	public static bool CanCancel(this OrderStatus status)
	{
		return status is OrderStatus.Pending or OrderStatus.Confirmed;
	}

	/// <summary>
	/// Checks if the order status allows updates
	/// </summary>
	public static bool CanUpdateStatus(this OrderStatus status)
	{
		return status != OrderStatus.Cancelled && status != OrderStatus.Delivered;
	}

	/// <summary>
	/// Gets valid next statuses for the current status
	/// </summary>
	public static IEnumerable<OrderStatus> GetValidNextStatuses(this OrderStatus currentStatus)
	{
		return currentStatus switch
		{
			OrderStatus.Pending => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
			OrderStatus.Confirmed => new[] { OrderStatus.Processing, OrderStatus.Cancelled },
			OrderStatus.Processing => new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
			OrderStatus.Shipped => new[] { OrderStatus.Delivered },
			OrderStatus.Delivered => Array.Empty<OrderStatus>(),
			OrderStatus.Cancelled => Array.Empty<OrderStatus>(),
			_ => Array.Empty<OrderStatus>()
		};
	}

	/// <summary>
	/// Validates if a transition from current status to new status is allowed
	/// </summary>
	public static bool IsValidTransition(this OrderStatus currentStatus, OrderStatus newStatus)
	{
		return GetValidNextStatuses(currentStatus).Contains(newStatus);
	}
}
