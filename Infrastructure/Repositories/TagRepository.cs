using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з тегами
/// </summary>
public class TagRepository : ITagRepository
{
	private readonly AppDbContext _db;

	public TagRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<IEnumerable<Tag>> GetAllAsync()
	{
		return await _db.Tags
			.Include(t => t.ProductTags)
			.ToListAsync();
	}

	public async Task<Tag?> GetByIdAsync(Guid id)
	{
		return await _db.Tags
			.Include(t => t.ProductTags)
			.FirstOrDefaultAsync(t => t.Id == id);
	}

	/// <inheritdoc />
	public async Task<Tag?> GetByIdLightAsync(Guid id)
	{
		return await _db.Tags
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == id);
	}

	/// <inheritdoc />
	public async Task<bool> ExistsAsync(Guid id)
	{
		return await _db.Tags.AnyAsync(t => t.Id == id);
	}

	public async Task<Tag?> GetBySlugAsync(string slug)
	{
		if (string.IsNullOrWhiteSpace(slug))
		{
			return null;
		}

		var normalized = slug.Trim();
		return await _db.Tags
			.Include(t => t.ProductTags)
			.FirstOrDefaultAsync(t => t.Slug == normalized);
	}

	public async Task<Tag?> GetByNameAsync(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		var normalized = name.Trim();
		return await _db.Tags
			.FirstOrDefaultAsync(t => t.Name == normalized);
	}

	public void Add(Tag tag)
	{
		_db.Tags.Add(tag);
	}

	public void Update(Tag tag)
	{
		_db.Tags.Update(tag);
	}

	public void Delete(Tag tag)
	{
		_db.Tags.Remove(tag);
	}

	public Task DeleteAsync(Tag tag)
	{
		_db.Tags.Remove(tag);
		return Task.CompletedTask;
	}

	public async Task SaveChangesAsync()
	{
		await _db.SaveChangesAsync();
	}
}
