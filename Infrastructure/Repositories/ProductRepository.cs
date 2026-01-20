using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
				.ThenInclude(s => s.Gallery)
					.ThenInclude(g => g.MediaImage)
			.Include(p => p.Skus)
				.ThenInclude(s => s.AttributeValues)
					.ThenInclude(av => av.AttributeDefinition)
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

	public async Task<Product?> GetBySlugAsync(string slug)
	{
		if (string.IsNullOrWhiteSpace(slug))
		{
			return null;
		}

		var normalized = slug.Trim().ToLowerInvariant();
		return await WithDetails()
			.FirstOrDefaultAsync(p => p.Slug.ToLower() == normalized);
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

	public async Task<IEnumerable<Product>> SearchAsync(string query, int limit = 20)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return Enumerable.Empty<Product>();
		}

		var normalized = query.Trim().ToLowerInvariant();

		return await WithDetails()
			.Where(p => p.IsActive && 
			            p.Store != null && 
			            p.Store.IsVerified && 
			            !p.Store.IsSuspended &&
			            (p.Name.ToLower().Contains(normalized) ||
			             (p.Description != null && p.Description.ToLower().Contains(normalized)) ||
			             p.Skus.Any(s => s.SkuCode.ToLower().Contains(normalized))))
			.OrderByDescending(p => p.Name.ToLower().StartsWith(normalized)) // Exact prefix matches first
			.ThenByDescending(p => p.CreatedAt)
			.Take(limit)
			.ToListAsync();
	}

	public async Task<IEnumerable<Product>> GetActiveByCategoryIdWithSkusAsync(Guid categoryId)
	{
		return await _db.Products
			.Include(p => p.Skus)
				.ThenInclude(s => s.AttributeValues)
					.ThenInclude(av => av.AttributeDefinition)
			.Include(p => p.Store)
			.Where(p => p.IsActive && 
			            p.Store != null && 
			            p.Store.IsVerified && 
			            !p.Store.IsSuspended &&
			            p.ProductCategories.Any(pc => pc.CategoryId == categoryId))
			.ToListAsync();
	}

	/// <summary>
	/// Filters products with efficient database-level filtering using typed attribute values.
	/// All filters (including numeric) are now executed at the database level for maximum performance.
	/// </summary>
	public async Task<(IEnumerable<Product> Products, int TotalCount)> FilterAsync(
		Guid? categoryId,
		List<Guid>? tagIds,
		decimal? minPrice,
		decimal? maxPrice,
		bool? inStock,
		Dictionary<string, object>? attributeFilters,
		string sort,
		int page,
		int pageSize)
	{
		// Start with base query - active products from verified stores
		var query = _db.Products
			.Include(p => p.Skus)
				.ThenInclude(s => s.AttributeValues) // 🚀 Include typed attributes
					.ThenInclude(av => av.AttributeDefinition)
			.Include(p => p.Store)
			.Include(p => p.ProductCategories)
				.ThenInclude(pc => pc.Category)
			.Include(p => p.ProductTags)
				.ThenInclude(pt => pt.Tag)
			.Where(p => p.IsActive && 
			            p.Store != null && 
			            p.Store.IsVerified && 
			            !p.Store.IsSuspended)
			.AsQueryable();

		// Filter by category
		if (categoryId.HasValue && categoryId.Value != Guid.Empty)
		{
			query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId.Value));
		}

		// Filter by tags (AND logic - product must have ALL specified tags)
		if (tagIds is not null && tagIds.Count > 0)
		{
			foreach (var tagId in tagIds)
			{
				var currentTagId = tagId;
				query = query.Where(p => p.ProductTags.Any(pt => pt.TagId == currentTagId));
			}
		}

		// Filter by price range (check any SKU in product matches)
		if (minPrice.HasValue)
		{
			query = query.Where(p => p.Skus.Any(s => s.Price >= minPrice.Value));
		}
		if (maxPrice.HasValue)
		{
			query = query.Where(p => p.Skus.Any(s => s.Price <= maxPrice.Value));
		}

		// Filter by stock availability
		if (inStock.HasValue && inStock.Value)
		{
			query = query.Where(p => p.Skus.Any(s => s.StockQuantity > 0));
		}

		// ⭐ TYPED ATTRIBUTE FILTERING - All done at database level! ⭐
		if (attributeFilters is not null && attributeFilters.Count > 0)
		{
			foreach (var (attributeCode, filterValue) in attributeFilters)
			{
				var code = attributeCode;
				var filter = filterValue;

				if (filter is Dictionary<string, object> filterDict)
				{
					// String IN filter: Check if product has any SKU with attribute value in list
					if (filterDict.TryGetValue("In", out var inValues) && inValues is List<string> inList && inList.Count > 0)
					{
						query = query.Where(p => p.Skus.Any(s => 
							s.AttributeValues.Any(av => 
								av.AttributeDefinition.Code == code &&
								av.ValueString != null &&
								inList.Contains(av.ValueString)
							)
						));
					}

					// String EQUAL filter: Exact string match
					if (filterDict.TryGetValue("Equal", out var equalValue) && equalValue is string equalStr && !string.IsNullOrEmpty(equalStr))
					{
						query = query.Where(p => p.Skus.Any(s => 
							s.AttributeValues.Any(av => 
								av.AttributeDefinition.Code == code &&
								av.ValueString == equalStr
							)
						));
					}

					// 🚀 Numeric GTE filter (database-level!)
					if (filterDict.TryGetValue("Gte", out var gteValue) && gteValue is decimal gteNum)
					{
						query = query.Where(p => p.Skus.Any(s => 
							s.AttributeValues.Any(av => 
								av.AttributeDefinition.Code == code &&
								av.ValueNumber.HasValue &&
								av.ValueNumber.Value >= gteNum
							)
						));
					}

					// 🚀 Numeric LTE filter (database-level!)
					if (filterDict.TryGetValue("Lte", out var lteValue) && lteValue is decimal lteNum)
					{
						query = query.Where(p => p.Skus.Any(s => 
							s.AttributeValues.Any(av => 
								av.AttributeDefinition.Code == code &&
								av.ValueNumber.HasValue &&
								av.ValueNumber.Value <= lteNum
							)
						));
					}

					// 🚀 Numeric EQ filter (database-level!)
					if (filterDict.TryGetValue("Eq", out var eqValue) && eqValue is decimal eqNum)
					{
						query = query.Where(p => p.Skus.Any(s => 
							s.AttributeValues.Any(av => 
								av.AttributeDefinition.Code == code &&
								av.ValueNumber.HasValue &&
								av.ValueNumber.Value == eqNum
							)
						));
					}

					// Boolean filter
					if (filterDict.TryGetValue("Equal", out var boolValue) && boolValue is bool boolVal)
					{
						query = query.Where(p => p.Skus.Any(s => 
							s.AttributeValues.Any(av => 
								av.AttributeDefinition.Code == code &&
								av.ValueBoolean.HasValue &&
								av.ValueBoolean.Value == boolVal
							)
						));
					}
				}
			}
		}

		// Get accurate total count after ALL filters
		var totalCount = await query.CountAsync();

		// Apply sorting
		query = sort.ToLowerInvariant() switch
		{
			"newest" => query.OrderByDescending(p => p.CreatedAt),
			"priceasc" => query.OrderBy(p => p.Skus.Min(s => s.Price)),
			"pricedesc" => query.OrderByDescending(p => p.Skus.Max(s => s.Price)),
			_ => query.OrderByDescending(p => p.CreatedAt) // Default to newest
		};

		// Apply pagination - no need to fetch extra since all filtering is at DB level
		var products = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		return (products, totalCount);
	}

	public void Add(Product product)
	{
		_db.Products.Add(product);
	}

	public void Update(Product product)
	{
		_db.Products.Update(product);
	}

	public void Delete(Product product)
	{
		_db.Products.Remove(product);
	}

	/// <inheritdoc />
	public void AddProductCategory(ProductCategory productCategory)
	{
		_db.Set<ProductCategory>().Add(productCategory);
	}

	/// <inheritdoc />
	public void RemoveProductCategory(ProductCategory productCategory)
	{
		_db.Set<ProductCategory>().Remove(productCategory);
	}

	/// <inheritdoc />
	public void AddProductTag(ProductTag productTag)
	{
		_db.Set<ProductTag>().Add(productTag);
	}

	/// <inheritdoc />
	public void RemoveProductTag(ProductTag productTag)
	{
		_db.Set<ProductTag>().Remove(productTag);
	}
}
