using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з продуктами
/// </summary>
public class ProductRepository : IProductRepository
{
	private readonly AppDbContext _db;

	public ProductRepository(AppDbContext db)
	{
		_db = db;
	}

	private IQueryable<Product> WithDetails()
	{
		return _db.Products
			.Include(p => p.Store)
			.Include(p => p.Skus)
			.Include(p => p.Gallery)
				.ThenInclude(g => g.MediaImage)
			.Include(p => p.ProductCategories)
				.ThenInclude(pc => pc.Category)
			.Include(p => p.ProductTags)
				.ThenInclude(pt => pt.Tag);
	}

	public async Task<Product?> GetByIdAsync(Guid id)
	{
		return await WithDetails().FirstOrDefaultAsync(p => p.Id == id);
	}

	public async Task<IEnumerable<Product>> GetAllAsync()
	{
		return await WithDetails().ToListAsync();
	}

	public async Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId)
	{
		return await WithDetails()
			.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId))
			.ToListAsync();
	}

	public async Task<IEnumerable<Product>> GetByStoreIdAsync(Guid storeId)
	{
		return await WithDetails()
			.Where(p => p.StoreId == storeId)
			.ToListAsync();
	}

	public async Task<Product?> GetBySkuCodeAsync(string skuCode)
	{
		if (string.IsNullOrWhiteSpace(skuCode))
		{
			return null;
		}

		var normalized = skuCode.Trim();
		return await WithDetails()
			.FirstOrDefaultAsync(p => p.Skus.Any(s => s.SkuCode == normalized));
	}

	public void Add(Product product)
	{
		_db.Products.Add(product);
	}



	public void Delete(Product product)
	{
		_db.Products.Remove(product);
	}
}
