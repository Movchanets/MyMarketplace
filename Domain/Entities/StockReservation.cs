using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a temporary stock reservation during checkout process
/// with automatic expiry to prevent stock from being held indefinitely
/// </summary>
public class StockReservation : BaseEntity<Guid>
{
	/// <summary>
	/// Reference to the SKU being reserved
	/// </summary>
	public Guid SkuId { get; private set; }

	/// <summary>
	/// The SKU entity being reserved
	/// </summary>
	public virtual SkuEntity? Sku { get; private set; }

	/// <summary>
	/// Reference to the cart that holds this reservation (if applicable)
	/// </summary>
	public Guid? CartId { get; private set; }

	/// <summary>
	/// Reference to the order that converted this reservation (if applicable)
	/// </summary>
	public Guid? OrderId { get; private set; }

	/// <summary>
	/// Quantity of stock being reserved
	/// </summary>
	public int Quantity { get; private set; }

	/// <summary>
	/// When the reservation was created
	/// </summary>
	public DateTime CreatedAt { get; private set; }

	/// <summary>
	/// When the reservation expires (default: 15 minutes from creation)
	/// </summary>
	public DateTime ExpiresAt { get; private set; }

	/// <summary>
	/// Current status of the reservation
	/// </summary>
	public ReservationStatus Status { get; private set; }

	/// <summary>
	/// Optional session identifier for guest checkouts
	/// </summary>
	public string? SessionId { get; private set; }

	/// <summary>
	/// IP address of the user who created the reservation
	/// </summary>
	public string? IpAddress { get; private set; }

	/// <summary>
	/// User agent of the client who created the reservation
	/// </summary>
	public string? UserAgent { get; private set; }

	/// <summary>
	/// When the reservation was last updated
	/// </summary>
	public DateTime? UpdatedAt { get; private set; }

	/// <summary>
	/// Reason for cancellation (if applicable)
	/// </summary>
	public string? CancellationReason { get; private set; }

	/// <summary>
	/// Default TTL for reservations in minutes
	/// </summary>
	public const int DefaultTtlMinutes = 15;

	private StockReservation() { }

	/// <summary>
	/// Creates a new stock reservation
	/// </summary>
	public StockReservation(
		Guid skuId,
		int quantity,
		Guid? cartId = null,
		string? sessionId = null,
		string? ipAddress = null,
		string? userAgent = null,
		int ttlMinutes = DefaultTtlMinutes)
	{
		if (skuId == Guid.Empty)
			throw new ArgumentException("SKU ID cannot be empty", nameof(skuId));

		if (quantity <= 0)
			throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");

		if (ttlMinutes <= 0)
			throw new ArgumentOutOfRangeException(nameof(ttlMinutes), "TTL must be greater than zero");

		Id = Guid.NewGuid();
		SkuId = skuId;
		Quantity = quantity;
		CartId = cartId;
		SessionId = sessionId;
		IpAddress = ipAddress;
		UserAgent = userAgent;
		Status = ReservationStatus.Active;
		CreatedAt = DateTime.UtcNow;
		ExpiresAt = CreatedAt.AddMinutes(ttlMinutes);
	}

	/// <summary>
	/// Checks if the reservation has expired
	/// </summary>
	public bool IsExpired()
	{
		return DateTime.UtcNow > ExpiresAt;
	}

	/// <summary>
	/// Gets the remaining time before expiry
	/// </summary>
	public TimeSpan GetTimeRemaining()
	{
		var remaining = ExpiresAt - DateTime.UtcNow;
		return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
	}

	/// <summary>
	/// Extends the reservation expiry time
	/// </summary>
	public void ExtendExpiry(int additionalMinutes)
	{
		if (Status != ReservationStatus.Active)
			throw new InvalidOperationException($"Cannot extend expiry for reservation with status {Status}");

		if (additionalMinutes <= 0)
			throw new ArgumentOutOfRangeException(nameof(additionalMinutes), "Additional minutes must be greater than zero");

		ExpiresAt = ExpiresAt.AddMinutes(additionalMinutes);
		UpdatedAt = DateTime.UtcNow;
		MarkAsUpdated();
	}

	/// <summary>
	/// Converts the reservation to an order deduction
	/// </summary>
	public void ConvertToOrder(Guid orderId)
	{
		if (!Status.CanConvert())
			throw new InvalidOperationException($"Cannot convert reservation with status {Status}");

		if (orderId == Guid.Empty)
			throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

		OrderId = orderId;
		Status = ReservationStatus.Converted;
		UpdatedAt = DateTime.UtcNow;
		MarkAsUpdated();
	}

	/// <summary>
	/// Cancels the reservation and releases the stock
	/// </summary>
	public void Cancel(string? reason = null)
	{
		if (!Status.CanCancel())
			throw new InvalidOperationException($"Cannot cancel reservation with status {Status}");

		Status = ReservationStatus.Cancelled;
		CancellationReason = reason;
		UpdatedAt = DateTime.UtcNow;
		MarkAsUpdated();
	}

	/// <summary>
	/// Marks the reservation as expired
	/// </summary>
	public void MarkAsExpired()
	{
		if (Status != ReservationStatus.Active)
			throw new InvalidOperationException($"Cannot mark reservation as expired with status {Status}");

		Status = ReservationStatus.Expired;
		UpdatedAt = DateTime.UtcNow;
		MarkAsUpdated();
	}

	/// <summary>
	/// Checks if the reservation is associated with a specific cart
	/// </summary>
	public bool IsForCart(Guid cartId)
	{
		return CartId.HasValue && CartId.Value == cartId;
	}

	/// <summary>
	/// Checks if the reservation is associated with a specific session
	/// </summary>
	public bool IsForSession(string sessionId)
	{
		return !string.IsNullOrEmpty(SessionId) && SessionId == sessionId;
	}
}
