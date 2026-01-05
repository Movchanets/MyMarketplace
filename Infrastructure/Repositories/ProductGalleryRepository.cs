using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з галереєю продуктів
/// </summary>
public class ProductGalleryRepository : IProductGalleryRepository
{
	private readonly AppDbContext _db;
	private readonly IFileStorage _fileStorage;

	public ProductGalleryRepository(AppDbContext db, IFileStorage fileStorage)
	{
		_db = db;
		_fileStorage = fileStorage;
	}

	public async Task<ProductGallery?> GetByIdAsync(Guid id)
	{
		return await _db.ProductGalleries
			.Include(g => g.MediaImage)
			.FirstOrDefaultAsync(g => g.Id == id);
	}

	public async Task<IEnumerable<ProductGallery>> GetByProductIdAsync(Guid productId)
	{
		return await _db.ProductGalleries
			.Include(g => g.MediaImage)
			.Where(g => g.ProductId == productId)
			.OrderBy(g => g.DisplayOrder)
			.ToListAsync();
	}

	public void Add(ProductGallery galleryItem)
	{
		_db.ProductGalleries.Add(galleryItem);
	}

	public void Delete(ProductGallery galleryItem)
	{
		_db.ProductGalleries.Remove(galleryItem);
	}

	public async Task<GalleryImageResult> UploadAndAddAsync(Product product, GalleryImageUploadRequest request)
	{
		// Upload file to storage
		var storageKey = await _fileStorage.UploadAsync(
			request.FileStream,
			$"products/{product.Id}/{request.FileName}",
			request.ContentType
		);

		// Create MediaImage
		var mediaImage = new MediaImage(
			storageKey,
			request.ContentType,
			0, 0, // Width/Height - can be extracted later
			request.FileName
		);
		await _db.MediaImages.AddAsync(mediaImage);

		// Create ProductGallery
		var gallery = ProductGallery.Create(product, mediaImage, request.DisplayOrder);
		_db.ProductGalleries.Add(gallery);

		var publicUrl = _fileStorage.GetPublicUrl(storageKey);

		return new GalleryImageResult(gallery.Id, mediaImage.Id, storageKey, publicUrl);
	}

	public async Task DeleteWithFileAsync(ProductGallery galleryItem)
	{
		// Delete file from storage
		if (galleryItem.MediaImage != null)
		{
			await _fileStorage.DeleteAsync(galleryItem.MediaImage.StorageKey);
		}

		// Remove from DB
		_db.ProductGalleries.Remove(galleryItem);
	}

	public string GetPublicUrl(string storageKey)
	{
		return _fileStorage.GetPublicUrl(storageKey);
	}
}
