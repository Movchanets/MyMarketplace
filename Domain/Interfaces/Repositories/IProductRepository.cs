using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository для роботи з продуктами (з урахуванням SKU)
/// </summary>
public interface IProductRepository
{
	Task<Product?> GetByIdAsync(Guid id);
	Task<IEnumerable<Product>> GetAllAsync();
	Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId);
	Task<IEnumerable<Product>> GetByStoreIdAsync(Guid storeId);
	Task<Product?> GetBySkuCodeAsync(string skuCode);

	void Add(Product product);

	void Delete(Product product);
}
