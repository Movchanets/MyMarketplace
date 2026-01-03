using System.Text.Json;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з SKU
/// </summary>
public class SkuRepository : ISkuRepository
{
	private readonly AppDbContext _db;

	public SkuRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<SkuEntity?> GetByIdAsync(Guid id)
	{
		if (id == Guid.Empty)
		{
			return null;
		}

		return await _db.Skus
			.Include(s => s.Product)
				.ThenInclude(p => p!.Store)
			.Include(s => s.Gallery)
				.ThenInclude(g => g.MediaImage)
			.FirstOrDefaultAsync(s => s.Id == id);
	}

	public async Task<SkuEntity?> GetBySkuCodeAsync(string skuCode)
	{
		if (string.IsNullOrWhiteSpace(skuCode))
		{
			return null;
		}

		var normalized = skuCode.Trim();
		return await _db.Skus
			.Include(s => s.Product)
				.ThenInclude(p => p!.Store)
			.Include(s => s.Gallery)
				.ThenInclude(g => g.MediaImage)
			.FirstOrDefaultAsync(s => s.SkuCode == normalized);
	}

	public async Task<IEnumerable<SkuEntity>> GetByProductIdAsync(Guid productId)
	{
		return await _db.Skus
			.Where(s => s.ProductId == productId)
			.Include(s => s.Product)
				.ThenInclude(p => p!.Store)
			.Include(s => s.Gallery)
				.ThenInclude(g => g.MediaImage)
			.ToListAsync();
	}

	public async Task<IEnumerable<SkuEntity>> GetByJsonAttributeAsync(string key, string value)
	{
		if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
		{
			return Array.Empty<SkuEntity>();
		}

		var payload = JsonSerializer.Serialize(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			[key.Trim()] = value.Trim()
		});

		// JSONB containment query: Attributes @> '{"key":"value"}'::jsonb
		return await _db.Skus
			.FromSqlInterpolated($"SELECT * FROM \"Skus\" WHERE \"Attributes\" IS NOT NULL AND \"Attributes\" @> {payload}::jsonb")
			.Include(s => s.Product)
			.ToListAsync();
	}

	public async Task<IEnumerable<SkuEntity>> SearchBySkuCodeAsync(string query, int take = 50)
	{
		if (string.IsNullOrWhiteSpace(query) || take <= 0)
		{
			return Array.Empty<SkuEntity>();
		}

		var q = query.Trim();
		return await _db.Skus
			.Where(s => s.SkuCode.Contains(q))
			.Include(s => s.Product)
			.Take(take)
			.ToListAsync();
	}

	public void Add(SkuEntity sku)
	{
		_db.Skus.Add(sku);
	}

	public void Update(SkuEntity sku)
	{
		_db.Skus.Update(sku);
	}

	public void Delete(SkuEntity sku)
	{
		_db.Skus.Remove(sku);
	}
}
