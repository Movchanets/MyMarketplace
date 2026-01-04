using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// DTO для завантаження зображення в галерею
/// </summary>
public sealed record GalleryImageUploadRequest(
	Stream FileStream,
	string FileName,
	string ContentType,
	int DisplayOrder
);

/// <summary>
/// Результат додавання зображення в галерею
/// </summary>
public sealed record GalleryImageResult(
	Guid GalleryId,
	Guid MediaImageId,
	string StorageKey,
	string PublicUrl
);

/// <summary>
/// Repository для роботи з галереєю продуктів
/// </summary>
public interface IProductGalleryRepository
{
	Task<ProductGallery?> GetByIdAsync(Guid id);
	Task<IEnumerable<ProductGallery>> GetByProductIdAsync(Guid productId);
	void Add(ProductGallery galleryItem);
	void Delete(ProductGallery galleryItem);
	
	/// <summary>
	/// Завантажує зображення у сховище, створює MediaImage і додає до галереї продукту
	/// </summary>
	Task<GalleryImageResult> UploadAndAddAsync(Product product, GalleryImageUploadRequest request);
	
	/// <summary>
	/// Видаляє зображення зі сховища і з бази
	/// </summary>
	Task DeleteWithFileAsync(ProductGallery galleryItem);
	
	/// <summary>
	/// Отримує публічний URL для storage key
	/// </summary>
	string GetPublicUrl(string storageKey);
}
