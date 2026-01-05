using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з категоріями
/// </summary>
public class CategoryRepository : ICategoryRepository
{
	private readonly AppDbContext _db;

	public CategoryRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<Category?> GetByIdAsync(Guid id)
	{
		return await _db.Categories
			.Include(c => c.ParentCategory)
			.Include(c => c.Children)
			.Include(c => c.ProductCategories)
			.FirstOrDefaultAsync(c => c.Id == id);
	}

	/// <inheritdoc />
	public async Task<Category?> GetByIdLightAsync(Guid id)
	{
		return await _db.Categories
			.AsNoTracking()
			.Include(c => c.ParentCategory)
			.FirstOrDefaultAsync(c => c.Id == id);
	}

	/// <inheritdoc />
	public async Task<bool> ExistsAsync(Guid id)
	{
		return await _db.Categories.AnyAsync(c => c.Id == id);
	}

	public async Task<Category?> GetBySlugAsync(string slug)
	{
		if (string.IsNullOrWhiteSpace(slug))
		{
			return null;
		}

		var normalized = slug.Trim();
		return await _db.Categories
			.Include(c => c.ParentCategory)
			.Include(c => c.Children)
			.Include(c => c.ProductCategories)
			.FirstOrDefaultAsync(c => c.Slug == normalized);
	}

	public async Task<IEnumerable<Category>> GetAllAsync()
	{
		return await _db.Categories
			.Include(c => c.ParentCategory)
			.Include(c => c.Children)
			.ToListAsync();
	}

	public async Task<IEnumerable<Category>> GetTopLevelAsync()
	{
		return await _db.Categories
			.Where(c => c.ParentCategoryId == null)
			.Include(c => c.Children)
			.ToListAsync();
	}

	public async Task<IEnumerable<Category>> GetSubCategoriesAsync(Guid parentCategoryId)
	{
		return await _db.Categories
			.Where(c => c.ParentCategoryId == parentCategoryId)
			.Include(c => c.Children)
			.ToListAsync();
	}

	public void Add(Category category)
	{
		_db.Categories.Add(category);
	}

	public void Update(Category category)
	{
		_db.Categories.Update(category);
	}

	public void Delete(Category category)
	{
		_db.Categories.Remove(category);
	}
}
