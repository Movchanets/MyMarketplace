using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з магазинами
/// </summary>
public class StoreRepository : IStoreRepository
{
	private readonly AppDbContext _db;

	public StoreRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<Store?> GetByIdAsync(Guid id)
	{
		return await _db.Stores
			.Include(s => s.User)
			.Include(s => s.Products)
			.FirstOrDefaultAsync(s => s.Id == id);
	}

	public async Task<Store?> GetByUserIdAsync(Guid userId)
	{
		return await _db.Stores
			.Include(s => s.User)
			.Include(s => s.Products)
			.FirstOrDefaultAsync(s => s.UserId == userId);
	}

	public async Task<Store?> GetBySlugAsync(string slug)
	{
		if (string.IsNullOrWhiteSpace(slug))
		{
			return null;
		}

		var normalized = slug.Trim();
		return await _db.Stores
			.Include(s => s.User)
			.Include(s => s.Products)
				.ThenInclude(p => p.Skus)
			.Include(s => s.Products)
				.ThenInclude(p => p.ProductCategories)
					.ThenInclude(pc => pc.Category)
			.Include(s => s.Products)
				.ThenInclude(p => p.ProductTags)
					.ThenInclude(pt => pt.Tag)
			.FirstOrDefaultAsync(s => s.Slug == normalized);
	}

	public async Task<IEnumerable<Store>> GetAllAsync(bool includeUnverified = false)
	{
		var query = _db.Stores
			.Include(s => s.User)
			.Include(s => s.Products)
			.AsQueryable();

		if (!includeUnverified)
		{
			query = query.Where(s => s.IsVerified);
		}

		return await query.ToListAsync();
	}

	public void Add(Store store)
	{
		_db.Stores.Add(store);
	}

	public void Update(Store store)
	{
		_db.Stores.Update(store);
	}

	public void Delete(Store store)
	{
		_db.Stores.Remove(store);
	}
}
