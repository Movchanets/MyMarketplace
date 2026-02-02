namespace Domain.Entities;

/// <summary>
/// Order item entity capturing product details at the time of purchase for historical integrity
/// </summary>
public class OrderItem : BaseEntity<Guid>
{
	public Guid OrderId { get; private set; }
	public virtual Order? Order { get; private set; }

	/// <summary>
	/// Reference to the product (may be null if product is deleted)
	/// </summary>
	public Guid ProductId { get; private set; }
	public virtual Product? Product { get; private set; }

	/// <summary>
	/// Reference to the SKU (may be null if SKU is deleted)
	/// </summary>
	public Guid SkuId { get; private set; }
	public virtual SkuEntity? Sku { get; private set; }

	/// <summary>
	/// Quantity ordered
	/// </summary>
	public int Quantity { get; private set; }

	/// <summary>
	/// Price per unit at the time of purchase (historical snapshot)
	/// </summary>
	public decimal PriceAtPurchase { get; private set; }

	/// <summary>
	/// Product name at the time of purchase (historical snapshot)
	/// </summary>
	public string ProductNameSnapshot { get; private set; } = string.Empty;

	/// <summary>
	/// Product image URL at the time of purchase (historical snapshot)
	/// </summary>
	public string? ProductImageUrlSnapshot { get; private set; }

	/// <summary>
	/// SKU code at the time of purchase (historical snapshot)
	/// </summary>
	public string SkuCodeSnapshot { get; private set; } = string.Empty;

	/// <summary>
	/// SKU attributes at the time of purchase (historical snapshot)
	/// </summary>
	public string? SkuAttributesSnapshot { get; private set; }

	private OrderItem() { }

	public OrderItem(
		Guid orderId,
		Guid productId,
		Guid skuId,
		int quantity,
		decimal priceAtPurchase,
		string productNameSnapshot,
		string? productImageUrlSnapshot,
		string skuCodeSnapshot,
		string? skuAttributesSnapshot)
	{
		if (orderId == Guid.Empty)
			throw new ArgumentException("OrderId cannot be empty", nameof(orderId));

		if (productId == Guid.Empty)
			throw new ArgumentException("ProductId cannot be empty", nameof(productId));

		if (skuId == Guid.Empty)
			throw new ArgumentException("SkuId cannot be empty", nameof(skuId));

		if (quantity <= 0)
			throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

		if (priceAtPurchase < 0)
			throw new ArgumentOutOfRangeException(nameof(priceAtPurchase), "Price cannot be negative");

		if (string.IsNullOrWhiteSpace(productNameSnapshot))
			throw new ArgumentException("Product name snapshot is required", nameof(productNameSnapshot));

		if (string.IsNullOrWhiteSpace(skuCodeSnapshot))
			throw new ArgumentException("SKU code snapshot is required", nameof(skuCodeSnapshot));

		this.Id = Guid.NewGuid();
		this.OrderId = orderId;
		this.ProductId = productId;
		this.SkuId = skuId;
		this.Quantity = quantity;
		this.PriceAtPurchase = priceAtPurchase;
		this.ProductNameSnapshot = productNameSnapshot.Trim();
		this.ProductImageUrlSnapshot = productImageUrlSnapshot;
		this.SkuCodeSnapshot = skuCodeSnapshot.Trim();
		this.SkuAttributesSnapshot = skuAttributesSnapshot;
	}

	/// <summary>
	/// Calculates the subtotal for this order item
	/// </summary>
	public decimal GetSubtotal() => PriceAtPurchase * Quantity;

	/// <summary>
	/// Factory method to create an OrderItem from a CartItem with product details
	/// </summary>
	public static OrderItem FromCartItem(
		Guid orderId,
		CartItem cartItem,
		string productName,
		string? productImageUrl,
		string skuCode,
		string? skuAttributes,
		decimal price)
	{
		if (cartItem == null)
			throw new ArgumentNullException(nameof(cartItem));

		return new OrderItem(
			orderId,
			cartItem.ProductId,
			cartItem.SkuId,
			cartItem.Quantity,
			price,
			productName,
			productImageUrl,
			skuCode,
			skuAttributes);
	}
}
