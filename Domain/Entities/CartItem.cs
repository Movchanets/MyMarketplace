namespace Domain.Entities;

/// <summary>
/// Cart item entity representing a product SKU in the shopping cart
/// </summary>
public class CartItem : BaseEntity<Guid>
{
	public Guid CartId { get; private set; }
	public virtual Cart? Cart { get; private set; }

	public Guid ProductId { get; private set; }
	public virtual Product? Product { get; private set; }

	public Guid SkuId { get; private set; }
	public virtual SkuEntity? Sku { get; private set; }

	public int Quantity { get; private set; }

	/// <summary>
	/// Timestamp when the item was added to cart
	/// </summary>
	public DateTime AddedAt { get; private set; }

	private CartItem() { }

	public CartItem(Guid cartId, Guid productId, Guid skuId, int quantity)
	{
		if (cartId == Guid.Empty)
			throw new ArgumentException("CartId cannot be empty", nameof(cartId));

		if (productId == Guid.Empty)
			throw new ArgumentException("ProductId cannot be empty", nameof(productId));

		if (skuId == Guid.Empty)
			throw new ArgumentException("SkuId cannot be empty", nameof(skuId));

		if (quantity <= 0)
			throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

		Id = Guid.NewGuid();
		CartId = cartId;
		ProductId = productId;
		SkuId = skuId;
		Quantity = quantity;
		AddedAt = DateTime.UtcNow;
	}

	/// <summary>
	/// Updates the quantity of the cart item
	/// </summary>
	public void UpdateQuantity(int newQuantity)
	{
		if (newQuantity <= 0)
			throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

		Quantity = newQuantity;
		MarkAsUpdated();
	}

	/// <summary>
	/// Calculates the subtotal for this cart item based on SKU price
	/// </summary>
	public decimal GetSubtotal()
	{
		if (Sku == null)
			throw new InvalidOperationException("SKU details must be loaded to calculate subtotal");

		return Sku.Price * Quantity;
	}
}
