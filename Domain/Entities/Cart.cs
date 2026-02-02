namespace Domain.Entities;

/// <summary>
/// Shopping cart aggregate root for a user
/// </summary>
public class Cart : BaseEntity<Guid>
{
	public Guid UserId { get; private set; }
	public virtual User? User { get; private set; }

	private readonly List<CartItem> _items = new();
	public virtual IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

	/// <summary>
	/// Row version for optimistic concurrency control
	/// </summary>
	public byte[]? RowVersion { get; private set; }

	private Cart() { }

	public Cart(Guid userId)
	{
		if (userId == Guid.Empty)
			throw new ArgumentException("UserId cannot be empty", nameof(userId));

		Id = Guid.NewGuid();
		UserId = userId;
	}

	/// <summary>
	/// Adds or updates an item in the cart
	/// </summary>
	public void AddItem(Guid productId, Guid skuId, int quantity)
	{
		if (quantity <= 0)
			throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

		var existingItem = _items.FirstOrDefault(i => i.SkuId == skuId);

		if (existingItem != null)
		{
			existingItem.UpdateQuantity(existingItem.Quantity + quantity);
		}
		else
		{
			_items.Add(new CartItem(Id, productId, skuId, quantity));
		}
		// Note: Not calling MarkAsUpdated() - cart changes are tracked via CartItem timestamps
	}

	/// <summary>
	/// Updates the quantity of an existing cart item
	/// </summary>
	public void UpdateItemQuantity(Guid cartItemId, int newQuantity)
	{
		if (newQuantity < 0)
			throw new ArgumentException("Quantity cannot be negative", nameof(newQuantity));

		var item = _items.FirstOrDefault(i => i.Id == cartItemId);

		if (item == null)
			throw new InvalidOperationException($"Cart item with ID {cartItemId} not found");

		if (newQuantity == 0)
		{
			_items.Remove(item);
		}
		else
		{
			item.UpdateQuantity(newQuantity);
		}
	}

	/// <summary>
	/// Updates the quantity of an item by SKU ID
	/// </summary>
	public void UpdateItemQuantityBySku(Guid skuId, int newQuantity)
	{
		if (newQuantity < 0)
			throw new ArgumentException("Quantity cannot be negative", nameof(newQuantity));

		var item = _items.FirstOrDefault(i => i.SkuId == skuId);

		if (item == null)
			throw new InvalidOperationException($"Cart item with SKU ID {skuId} not found");

		if (newQuantity == 0)
		{
			_items.Remove(item);
		}
		else
		{
			item.UpdateQuantity(newQuantity);
		}
	}

	/// <summary>
	/// Removes an item from the cart
	/// </summary>
	public void RemoveItem(Guid cartItemId)
	{
		var item = _items.FirstOrDefault(i => i.Id == cartItemId);

		if (item != null)
		{
			_items.Remove(item);
		}
	}

	/// <summary>
	/// Removes an item from the cart by SKU ID
	/// </summary>
	public void RemoveItemBySku(Guid skuId)
	{
		var item = _items.FirstOrDefault(i => i.SkuId == skuId);

		if (item != null)
		{
			_items.Remove(item);
		}
	}

	/// <summary>
	/// Clears all items from the cart
	/// </summary>
	public void Clear()
	{
		_items.Clear();
	}

	/// <summary>
	/// Gets the total number of items in the cart
	/// </summary>
	public int GetTotalItems() => _items.Sum(i => i.Quantity);

	/// <summary>
	/// Checks if the cart contains an item with the specified SKU
	/// </summary>
	public bool ContainsSku(Guid skuId) => _items.Any(i => i.SkuId == skuId);

	/// <summary>
	/// Gets the quantity of a specific SKU in the cart
	/// </summary>
	public int GetSkuQuantity(Guid skuId)
	{
		var item = _items.FirstOrDefault(i => i.SkuId == skuId);
		return item?.Quantity ?? 0;
	}
}
