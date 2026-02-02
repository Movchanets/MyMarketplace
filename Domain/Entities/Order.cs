using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Order aggregate root representing a customer order
/// </summary>
public class Order : BaseEntity<Guid>
{
	public Guid UserId { get; private set; }
	public virtual User? User { get; private set; }

	/// <summary>
	/// Unique order number for customer reference
	/// </summary>
	public string OrderNumber { get; private set; } = string.Empty;

	private readonly List<OrderItem> _items = new();
	public virtual IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

	/// <summary>
	/// Total price of the order (calculated from items)
	/// </summary>
	public decimal TotalPrice { get; private set; }

	/// <summary>
	/// Current status of the order
	/// </summary>
	public OrderStatus Status { get; private set; }

	/// <summary>
	/// Payment status of the order
	/// </summary>
	public PaymentStatus PaymentStatus { get; private set; }

	/// <summary>
	/// Shipping address for the order
	/// </summary>
	public ShippingAddress ShippingAddress { get; private set; } = null!;

	/// <summary>
	/// Selected delivery method
	/// </summary>
	public string DeliveryMethod { get; private set; } = string.Empty;

	/// <summary>
	/// Selected payment method
	/// </summary>
	public string PaymentMethod { get; private set; } = string.Empty;

	/// <summary>
	/// Optional promotional code applied to the order
	/// </summary>
	public string? PromoCode { get; private set; }

	/// <summary>
	/// Discount amount applied to the order
	/// </summary>
	public decimal DiscountAmount { get; private set; }

	/// <summary>
	/// Shipping cost for the order
	/// </summary>
	public decimal ShippingCost { get; private set; }

	/// <summary>
	/// Notes provided by the customer
	/// </summary>
	public string? CustomerNotes { get; private set; }

	/// <summary>
	/// Tracking number for shipped orders
	/// </summary>
	public string? TrackingNumber { get; private set; }

	/// <summary>
	/// Carrier used for shipping
	/// </summary>
	public string? ShippingCarrier { get; private set; }

	/// <summary>
	/// When the order was shipped
	/// </summary>
	public DateTime? ShippedAt { get; private set; }

	/// <summary>
	/// When the order was delivered
	/// </summary>
	public DateTime? DeliveredAt { get; private set; }

	/// <summary>
	/// When the order was cancelled
	/// </summary>
	public DateTime? CancelledAt { get; private set; }

	/// <summary>
	/// Reason for cancellation
	/// </summary>
	public string? CancellationReason { get; private set; }

	/// <summary>
	/// Idempotency key to prevent duplicate orders
	/// </summary>
	public string? IdempotencyKey { get; private set; }

	/// <summary>
	/// Row version for optimistic concurrency control
	/// </summary>
	public byte[]? RowVersion { get; private set; }

	private Order() { }

	public Order(
		Guid userId,
		ShippingAddress shippingAddress,
		string deliveryMethod,
		string paymentMethod,
		string? idempotencyKey = null)
	{
		if (userId == Guid.Empty)
			throw new ArgumentException("UserId cannot be empty", nameof(userId));

		if (shippingAddress == null)
			throw new ArgumentNullException(nameof(shippingAddress));

		if (string.IsNullOrWhiteSpace(deliveryMethod))
			throw new ArgumentException("Delivery method is required", nameof(deliveryMethod));

		if (string.IsNullOrWhiteSpace(paymentMethod))
			throw new ArgumentException("Payment method is required", nameof(paymentMethod));

		Id = Guid.NewGuid();
		UserId = userId;
		OrderNumber = GenerateOrderNumber();
		ShippingAddress = shippingAddress;
		DeliveryMethod = deliveryMethod.Trim();
		PaymentMethod = paymentMethod.Trim();
		Status = OrderStatus.Pending;
		PaymentStatus = PaymentStatus.Pending;
		IdempotencyKey = idempotencyKey;
	}

	/// <summary>
	/// Generates a unique order number
	/// </summary>
	private static string GenerateOrderNumber()
	{
		var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
		var random = new Random();
		var randomPart = random.Next(10000, 99999);
		return $"ORD-{timestamp}-{randomPart}";
	}

	/// <summary>
	/// Adds an item to the order
	/// </summary>
	public void AddItem(OrderItem item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));

		if (Status != OrderStatus.Pending)
			throw new InvalidOperationException("Cannot add items to an order that is not pending");

		_items.Add(item);
		RecalculateTotal();
		MarkAsUpdated();
	}

	/// <summary>
	/// Adds multiple items to the order
	/// </summary>
	public void AddItems(IEnumerable<OrderItem> items)
	{
		if (items == null)
			throw new ArgumentNullException(nameof(items));

		foreach (var item in items)
		{
			AddItem(item);
		}
	}

	/// <summary>
	/// Applies a promotional code to the order
	/// </summary>
	public void ApplyPromoCode(string promoCode, decimal discountAmount)
	{
		if (string.IsNullOrWhiteSpace(promoCode))
			throw new ArgumentException("Promo code cannot be empty", nameof(promoCode));

		if (discountAmount < 0)
			throw new ArgumentException("Discount amount cannot be negative", nameof(discountAmount));

		if (Status != OrderStatus.Pending)
			throw new InvalidOperationException("Cannot apply promo code to an order that is not pending");

		PromoCode = promoCode.Trim();
		DiscountAmount = discountAmount;
		RecalculateTotal();
		MarkAsUpdated();
	}

	/// <summary>
	/// Sets the shipping cost for the order
	/// </summary>
	public void SetShippingCost(decimal shippingCost)
	{
		if (shippingCost < 0)
			throw new ArgumentException("Shipping cost cannot be negative", nameof(shippingCost));

		ShippingCost = shippingCost;
		RecalculateTotal();
		MarkAsUpdated();
	}

	/// <summary>
	/// Sets customer notes for the order
	/// </summary>
	public void SetCustomerNotes(string? notes)
	{
		CustomerNotes = notes?.Trim();
		MarkAsUpdated();
	}

	/// <summary>
	/// Updates the order status with state machine validation
	/// </summary>
	public void UpdateStatus(OrderStatus newStatus)
	{
		if (!Status.IsValidTransition(newStatus))
			throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");

		Status = newStatus;

		// Set timestamps based on status
		switch (newStatus)
		{
			case OrderStatus.Shipped:
				ShippedAt = DateTime.UtcNow;
				break;
			case OrderStatus.Delivered:
				DeliveredAt = DateTime.UtcNow;
				break;
		}

		MarkAsUpdated();
	}

	/// <summary>
	/// Updates the payment status
	/// </summary>
	public void UpdatePaymentStatus(PaymentStatus newPaymentStatus)
	{
		PaymentStatus = newPaymentStatus;
		MarkAsUpdated();
	}

	/// <summary>
	/// Cancels the order with an optional reason
	/// </summary>
	public void Cancel(string? reason = null)
	{
		if (!Status.CanCancel())
			throw new InvalidOperationException($"Cannot cancel order with status {Status}");

		Status = OrderStatus.Cancelled;
		CancelledAt = DateTime.UtcNow;
		CancellationReason = reason?.Trim();
		MarkAsUpdated();
	}

	/// <summary>
	/// Sets shipping tracking information
	/// </summary>
	public void SetTrackingInfo(string trackingNumber, string carrier)
	{
		if (string.IsNullOrWhiteSpace(trackingNumber))
			throw new ArgumentException("Tracking number is required", nameof(trackingNumber));

		if (string.IsNullOrWhiteSpace(carrier))
			throw new ArgumentException("Carrier is required", nameof(carrier));

		TrackingNumber = trackingNumber.Trim();
		ShippingCarrier = carrier.Trim();
		MarkAsUpdated();
	}

	/// <summary>
	/// Recalculates the total price based on items, shipping, and discounts
	/// </summary>
	private void RecalculateTotal()
	{
		var itemsTotal = _items.Sum(i => i.GetSubtotal());
		TotalPrice = itemsTotal + ShippingCost - DiscountAmount;

		if (TotalPrice < 0)
			TotalPrice = 0;
	}

	/// <summary>
	/// Gets the total number of items in the order
	/// </summary>
	public int GetTotalItems() => _items.Sum(i => i.Quantity);

	/// <summary>
	/// Factory method to create an order from a cart
	/// </summary>
	public static Order FromCart(
		Cart cart,
		ShippingAddress shippingAddress,
		string deliveryMethod,
		string paymentMethod,
		Func<Guid, (string name, string? imageUrl, string skuCode, string? attributes, decimal price)> getProductDetails,
		string? idempotencyKey = null)
	{
		if (cart == null)
			throw new ArgumentNullException(nameof(cart));

		if (!cart.Items.Any())
			throw new InvalidOperationException("Cannot create order from empty cart");

		var order = new Order(
			cart.UserId,
			shippingAddress,
			deliveryMethod,
			paymentMethod,
			idempotencyKey);

		foreach (var cartItem in cart.Items)
		{
			var details = getProductDetails(cartItem.SkuId);
			var orderItem = OrderItem.FromCartItem(
				order.Id,
				cartItem,
				details.name,
				details.imageUrl,
				details.skuCode,
				details.attributes,
				details.price);

			order._items.Add(orderItem);
		}

		order.RecalculateTotal();
		return order;
	}
}
