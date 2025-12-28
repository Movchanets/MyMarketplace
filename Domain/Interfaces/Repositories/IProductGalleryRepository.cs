using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository для роботи з галереєю продуктів
/// </summary>
public interface IProductGalleryRepository
{
	Task<ProductGallery?> GetByIdAsync(Guid id);
	Task<IEnumerable<ProductGallery>> GetByProductIdAsync(Guid productId);
	void Add(ProductGallery galleryItem);
	void Delete(ProductGallery galleryItem);
}
