using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з галереєю SKU варіантів
/// </summary>
public class SkuGalleryRepository : ISkuGalleryRepository
{
	private readonly AppDbContext _db;
	private readonly IFileStorage _fileStorage;

	public SkuGalleryRepository(AppDbContext db, IFileStorage fileStorage)
	{
		_db = db;
		_fileStorage = fileStorage;
	}

	public async Task<SkuGallery?> GetByIdAsync(Guid id)
	{
		return await _db.SkuGalleries
			.Include(g => g.MediaImage)
			.FirstOrDefaultAsync(g => g.Id == id);
	}

	public async Task<IEnumerable<SkuGallery>> GetBySkuIdAsync(Guid skuId)
	{
		return await _db.SkuGalleries
			.Include(g => g.MediaImage)
			.Where(g => g.SkuId == skuId)
			.OrderBy(g => g.DisplayOrder)
			.ToListAsync();
	}

	public void Add(SkuGallery galleryItem)
	{
		_db.SkuGalleries.Add(galleryItem);
	}

	public void Delete(SkuGallery galleryItem)
	{
		_db.SkuGalleries.Remove(galleryItem);
	}

	public async Task DeleteWithFileAsync(SkuGallery galleryItem)
	{
		// Delete file from storage
		if (galleryItem.MediaImage != null)
		{
			await _fileStorage.DeleteAsync(galleryItem.MediaImage.StorageKey);
		}

		// Remove from DB
		_db.SkuGalleries.Remove(galleryItem);
	}

	public string GetPublicUrl(string storageKey)
	{
		return _fileStorage.GetPublicUrl(storageKey);
	}
}
