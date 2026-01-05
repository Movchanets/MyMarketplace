using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository для роботи з категоріями
/// </summary>
public interface ICategoryRepository
{
	Task<Category?> GetByIdAsync(Guid id);

	/// <summary>
	/// Gets category without loading ProductCategories collection.
	/// Use this when adding categories to products to avoid EF tracking conflicts.
	/// </summary>
	Task<Category?> GetByIdLightAsync(Guid id);

	/// <summary>
	/// Checks if a category exists without loading it into memory.
	/// </summary>
	Task<bool> ExistsAsync(Guid id);

	Task<Category?> GetBySlugAsync(string slug);
	Task<IEnumerable<Category>> GetAllAsync();
	Task<IEnumerable<Category>> GetTopLevelAsync();
	Task<IEnumerable<Category>> GetSubCategoriesAsync(Guid parentCategoryId);

	void Add(Category category);
	void Update(Category category);
	void Delete(Category category);
}
