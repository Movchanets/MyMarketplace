using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository для роботи з галереєю SKU варіантів
/// </summary>
public interface ISkuGalleryRepository
{
	Task<SkuGallery?> GetByIdAsync(Guid id);
	Task<IEnumerable<SkuGallery>> GetBySkuIdAsync(Guid skuId);
	void Add(SkuGallery galleryItem);
	void Delete(SkuGallery galleryItem);
	
	/// <summary>
	/// Видаляє зображення зі сховища і з бази
	/// </summary>
	Task DeleteWithFileAsync(SkuGallery galleryItem);
	
	/// <summary>
	/// Отримує публічний URL для storage key
	/// </summary>
	string GetPublicUrl(string storageKey);
}
