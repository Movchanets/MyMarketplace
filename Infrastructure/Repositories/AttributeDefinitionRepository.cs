using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AttributeDefinitionRepository : IAttributeDefinitionRepository
{
	private readonly AppDbContext _db;

	public AttributeDefinitionRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<AttributeDefinition?> GetByIdAsync(Guid id)
	{
		return await _db.AttributeDefinitions.FindAsync(id);
	}

	public async Task<AttributeDefinition?> GetByCodeAsync(string code)
	{
		if (string.IsNullOrWhiteSpace(code)) return null;

		var normalized = code.Trim().ToLowerInvariant();
		return await _db.AttributeDefinitions
			.FirstOrDefaultAsync(a => a.Code == normalized);
	}

	public async Task<IEnumerable<AttributeDefinition>> GetAllAsync(bool includeInactive = false)
	{
		var query = _db.AttributeDefinitions.AsQueryable();

		if (!includeInactive)
		{
			query = query.Where(a => a.IsActive);
		}

		return await query
			.OrderBy(a => a.DisplayOrder)
			.ThenBy(a => a.Name)
			.ToListAsync();
	}

	public async Task<IEnumerable<AttributeDefinition>> GetVariantAttributesAsync()
	{
		return await _db.AttributeDefinitions
			.Where(a => a.IsActive && a.IsVariant)
			.OrderBy(a => a.DisplayOrder)
			.ThenBy(a => a.Name)
			.ToListAsync();
	}

	public async Task<IEnumerable<AttributeDefinition>> GetByCodesAsync(IEnumerable<string> codes)
	{
		var normalizedCodes = codes
			.Where(c => !string.IsNullOrWhiteSpace(c))
			.Select(c => c.Trim().ToLowerInvariant())
			.ToList();

		return await _db.AttributeDefinitions
			.Where(a => normalizedCodes.Contains(a.Code))
			.ToListAsync();
	}

	public async Task<AttributeDefinition> AddAsync(AttributeDefinition attributeDefinition)
	{
		await _db.AttributeDefinitions.AddAsync(attributeDefinition);
		await _db.SaveChangesAsync();
		return attributeDefinition;
	}

	public async Task UpdateAsync(AttributeDefinition attributeDefinition)
	{
		_db.AttributeDefinitions.Update(attributeDefinition);
		await _db.SaveChangesAsync();
	}

	public async Task DeleteAsync(Guid id)
	{
		var entity = await _db.AttributeDefinitions.FindAsync(id);
		if (entity is not null)
		{
			_db.AttributeDefinitions.Remove(entity);
			await _db.SaveChangesAsync();
		}
	}

	public async Task<bool> ExistsAsync(string code)
	{
		if (string.IsNullOrWhiteSpace(code)) return false;

		var normalized = code.Trim().ToLowerInvariant();
		return await _db.AttributeDefinitions.AnyAsync(a => a.Code == normalized);
	}
}
