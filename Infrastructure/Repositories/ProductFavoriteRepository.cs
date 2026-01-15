using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProductFavoriteRepository : IProductFavoriteRepository
{
    private readonly AppDbContext _db;

    public ProductFavoriteRepository(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<ProductFavorite> WithDetails()
    {
        return _db.ProductFavorites
            .Include(f => f.Product)
                .ThenInclude(p => p!.Store)
            .Include(f => f.Product)
                .ThenInclude(p => p!.Skus)
            .Include(f => f.Product)
                .ThenInclude(p => p!.Gallery)
                    .ThenInclude(g => g.MediaImage);
    }

    public async Task<ProductFavorite?> GetByUserAndProductAsync(Guid userId, Guid productId)
    {
        return await WithDetails().FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
    }

    public async Task<IEnumerable<ProductFavorite>> GetByUserIdAsync(Guid userId)
    {
        return await WithDetails()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid productId)
    {
        return await _db.ProductFavorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId);
    }

    public async Task<int> GetCountByProductAsync(Guid productId)
    {
        return await _db.ProductFavorites.CountAsync(f => f.ProductId == productId);
    }

    public async Task<IEnumerable<ProductFavorite>> GetMostFavoritedAsync(int limit = 10)
    {
        return await _db.ProductFavorites
            .GroupBy(f => f.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .Join(_db.ProductFavorites.Include(f => f.Product),
                  stats => stats.ProductId,
                  fav => fav.ProductId,
                  (stats, fav) => fav)
            .Distinct()
            .ToListAsync();
    }

    public void Add(ProductFavorite favorite)
    {
        _db.ProductFavorites.Add(favorite);
    }

    public void Remove(ProductFavorite favorite)
    {
        _db.ProductFavorites.Remove(favorite);
    }
}