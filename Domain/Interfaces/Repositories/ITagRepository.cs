using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository для роботи з тегами
/// </summary>
public interface ITagRepository
{
	Task<IEnumerable<Tag>> GetAllAsync();
	Task<Tag?> GetByIdAsync(Guid id);

	/// <summary>
	/// Gets tag without loading ProductTags collection.
	/// Use this when adding tags to products to avoid EF tracking conflicts.
	/// </summary>
	Task<Tag?> GetByIdLightAsync(Guid id);

	/// <summary>
	/// Checks if a tag exists without loading it into memory.
	/// </summary>
	Task<bool> ExistsAsync(Guid id);

	Task<Tag?> GetBySlugAsync(string slug);
	Task<Tag?> GetByNameAsync(string name);

	void Add(Tag tag);
	void Update(Tag tag);
	void Delete(Tag tag);
	Task DeleteAsync(Tag tag);
	Task SaveChangesAsync();
}
