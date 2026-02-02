using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for Cart aggregate with async CRUD operations and ACID support
/// </summary>
public interface ICartRepository
{
	/// <summary>
	/// Gets a cart by its ID with all items
	/// </summary>
	Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a cart by user ID with all items
	/// </summary>
	Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a cart by user ID with all items, products, and SKU details loaded (for checkout/display)
	/// </summary>
	Task<Cart?> GetByUserIdWithProductsAsync(Guid userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a cart item by its ID
	/// </summary>
	Task<CartItem?> GetCartItemByIdAsync(Guid cartItemId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a cart item by cart id and sku id
	/// </summary>
	Task<CartItem?> GetCartItemByCartIdAndSkuAsync(Guid cartId, Guid skuId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new cart item
	/// </summary>
	void AddCartItem(CartItem cartItem);

	/// <summary>
	/// Updates an existing cart item
	/// </summary>
	void UpdateCartItem(CartItem cartItem);

	/// <summary>
	/// Checks if a user has a cart
	/// </summary>
	Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the count of items in a user's cart
	/// </summary>
	Task<int> GetItemCountAsync(Guid userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new cart
	/// </summary>
	void Add(Cart cart);

	/// <summary>
	/// Updates an existing cart
	/// </summary>
	void Update(Cart cart);

	/// <summary>
	/// Removes a cart
	/// </summary>
	void Remove(Cart cart);

	/// <summary>
	/// Removes a specific cart item
	/// </summary>
	void RemoveCartItem(CartItem cartItem);

	/// <summary>
	/// Clears all items from a cart
	/// </summary>
	Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes a cart by user ID
	/// </summary>
	Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
