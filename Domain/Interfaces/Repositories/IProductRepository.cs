using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository для роботи з продуктами (з урахуванням SKU)
/// </summary>
public interface IProductRepository
{
	Task<Product?> GetByIdAsync(Guid id);
	Task<Product?> GetBySlugAsync(string slug);
	Task<IEnumerable<Product>> GetAllAsync();
	Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId);
	Task<IEnumerable<Product>> GetByStoreIdAsync(Guid storeId);
	Task<Product?> GetBySkuCodeAsync(string skuCode);

	/// <summary>
	/// Searches products by query text (name, description, sku).
	/// Returns active products from verified stores.
	/// </summary>
	Task<IEnumerable<Product>> SearchAsync(string query, int limit = 20);

	/// <summary>
	/// Gets all active products in a category with SKUs loaded.
	/// Used for analyzing available filter options from actual product data.
	/// </summary>
	Task<IEnumerable<Product>> GetActiveByCategoryIdWithSkusAsync(Guid categoryId);

	/// <summary>
	/// Filters products based on category, tags, price, stock, and JSONB attributes.
	/// Returns paginated results with sorting.
	/// </summary>
	Task<(IEnumerable<Product> Products, int TotalCount)> FilterAsync(
		Guid? categoryId,
		List<Guid>? tagIds,
		decimal? minPrice,
		decimal? maxPrice,
		bool? inStock,
		Dictionary<string, object>? attributeFilters,
		string sort,
		int page,
		int pageSize
	);

	void Add(Product product);
	void Update(Product product);
	void Delete(Product product);

	/// <summary>
	/// Adds a ProductCategory link directly to the database context.
	/// Use this to avoid EF tracking conflicts when updating product categories.
	/// </summary>
	void AddProductCategory(ProductCategory productCategory);

	/// <summary>
	/// Removes a ProductCategory link directly from the database context.
	/// </summary>
	void RemoveProductCategory(ProductCategory productCategory);

	/// <summary>
	/// Adds a ProductTag link directly to the database context.
	/// </summary>
	void AddProductTag(ProductTag productTag);

	/// <summary>
	/// Removes a ProductTag link directly from the database context.
	/// </summary>
	void RemoveProductTag(ProductTag productTag);
}
