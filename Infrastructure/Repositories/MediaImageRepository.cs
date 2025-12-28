using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з MediaImage
/// </summary>
public class MediaImageRepository : IMediaImageRepository
{
	private readonly AppDbContext _db;

	public MediaImageRepository(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Отримує MediaImage за ID
	/// </summary>
	public async Task<MediaImage?> GetByIdAsync(Guid id)
	{
		return await _db.MediaImages
			.FirstOrDefaultAsync(m => m.Id == id);
	}

	/// <summary>
	/// Отримує MediaImage за ключем сховища
	/// </summary>
	public async Task<MediaImage?> GetByStorageKeyAsync(string storageKey)
	{
		return await _db.MediaImages
			.FirstOrDefaultAsync(m => m.StorageKey == storageKey);
	}

	/// <summary>
	/// Додає нове MediaImage в БД
	/// </summary>
	public void Add(MediaImage mediaImage)
	{
		_db.MediaImages.Add(mediaImage);
	}

	/// <summary>
	/// Додає нове MediaImage в БД асинхронно
	/// </summary>
	public async Task AddAsync(MediaImage mediaImage)
	{
		await _db.MediaImages.AddAsync(mediaImage);
		await _db.SaveChangesAsync();
	}

	/// <summary>
	/// Оновлює MediaImage в БД
	/// </summary>
	public void Update(MediaImage mediaImage)
	{
		_db.MediaImages.Update(mediaImage);;
	}

	/// <summary>
	/// Видаляє MediaImage з БД
	/// </summary>
	public void Delete(MediaImage mediaImage)
	{
		_db.MediaImages.Remove(mediaImage);
	}

	/// <summary>
	/// Отримує всі MediaImage
	/// </summary>
	public async Task<IEnumerable<MediaImage>> GetAllAsync()
	{
		return await _db.MediaImages.ToListAsync();
	}

	/// <summary>
	/// Отримує всі MediaImage для конкретного продукту
	/// </summary>
	public async Task<IEnumerable<MediaImage>> GetByProductIdAsync(Guid productId)
	{
		return await _db.ProductGalleries
			.Where(pg => pg.ProductId == productId)
			.Include(pg => pg.MediaImage)
			.Select(pg => pg.MediaImage!)
			.ToListAsync();
	}

	/// <summary>
	/// Отримує MediaImage, які не прив'язані до жодного продукту або користувача
	/// (orphaned images - можуть бути видалені)
	/// </summary>
	public async Task<IEnumerable<MediaImage>> GetOrphanedImagesAsync()
	{
		var usedInGalleryIds = await _db.ProductGalleries
			.Select(pg => pg.MediaImageId)
			.Distinct()
			.ToListAsync();

		var usedAvatarIds = await _db.DomainUsers
			.Where(u => u.AvatarId != null)
			.Select(u => u.AvatarId!.Value)
			.ToListAsync();

		return await _db.MediaImages
			.Where(m => !usedInGalleryIds.Contains(m.Id) && !usedAvatarIds.Contains(m.Id))
			.ToListAsync();
	}
}
