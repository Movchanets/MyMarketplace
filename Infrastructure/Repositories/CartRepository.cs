using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for Cart aggregate with optimistic concurrency and ACID support
/// </summary>
public class CartRepository : ICartRepository
{
	private readonly AppDbContext _db;

	public CartRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		return await _db.Carts
			.Include(c => c.Items)
			.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
	}

	public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		return await _db.Carts
			.Include(c => c.Items)
			.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
	}

	public async Task<Cart?> GetByUserIdWithProductsAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		return await _db.Carts
			.Include(c => c.Items)
				.ThenInclude(i => i.Product)
			.Include(c => c.Items)
				.ThenInclude(i => i.Sku)
			.AsSplitQuery()
			.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
	}

	public async Task<CartItem?> GetCartItemByIdAsync(Guid cartItemId, CancellationToken cancellationToken = default)
	{
		return await _db.CartItems
			.Include(ci => ci.Product)
			.Include(ci => ci.Sku)
			.FirstOrDefaultAsync(ci => ci.Id == cartItemId, cancellationToken);
	}

	public async Task<CartItem?> GetCartItemByCartIdAndSkuAsync(Guid cartId, Guid skuId, CancellationToken cancellationToken = default)
	{
		return await _db.CartItems
			.Include(ci => ci.Product)
			.Include(ci => ci.Sku)
			.FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.SkuId == skuId, cancellationToken);
	}

	public void AddCartItem(CartItem cartItem)
	{
		_db.CartItems.Add(cartItem);
	}

	public void UpdateCartItem(CartItem cartItem)
	{
		_db.CartItems.Update(cartItem);
	}

	public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		return await _db.Carts.AnyAsync(c => c.UserId == userId, cancellationToken);
	}

	public async Task<int> GetItemCountAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		return await _db.Carts
			.Where(c => c.UserId == userId)
			.SelectMany(c => c.Items)
			.SumAsync(i => i.Quantity, cancellationToken);
	}

	public void Add(Cart cart)
	{
		_db.Carts.Add(cart);
	}

	public void Update(Cart cart)
	{
		_db.Carts.Update(cart);
	}

	public void Remove(Cart cart)
	{
		_db.Carts.Remove(cart);
	}

	public void RemoveCartItem(CartItem cartItem)
	{
		_db.CartItems.Remove(cartItem);
	}

	public async Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken = default)
	{
		var cart = await GetByIdAsync(cartId, cancellationToken);
		if (cart != null)
		{
			cart.Clear();
		}
	}

	public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		var cart = await GetByUserIdAsync(userId, cancellationToken);
		if (cart != null)
		{
			Remove(cart);
		}
	}
}
