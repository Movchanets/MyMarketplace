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

	public ProductGalleryRepository(AppDbContext db)
	{
		_db = db;
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
}
