using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IAttributeDefinitionRepository
{
	Task<AttributeDefinition?> GetByIdAsync(Guid id);
	Task<AttributeDefinition?> GetByCodeAsync(string code);
	Task<IEnumerable<AttributeDefinition>> GetAllAsync(bool includeInactive = false);
	Task<IEnumerable<AttributeDefinition>> GetVariantAttributesAsync();
	Task<IEnumerable<AttributeDefinition>> GetByCodesAsync(IEnumerable<string> codes);
	Task<AttributeDefinition> AddAsync(AttributeDefinition attributeDefinition);
	Task UpdateAsync(AttributeDefinition attributeDefinition);
	Task DeleteAsync(Guid id);
	Task<bool> ExistsAsync(string code);
}
