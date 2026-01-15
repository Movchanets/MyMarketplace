using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IProductFavoriteRepository
{
    Task<ProductFavorite?> GetByUserAndProductAsync(Guid userId, Guid productId);
    Task<IEnumerable<ProductFavorite>> GetByUserIdAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId, Guid productId);
    Task<int> GetCountByProductAsync(Guid productId);
    Task<IEnumerable<ProductFavorite>> GetMostFavoritedAsync(int limit = 10);
    void Add(ProductFavorite favorite);
    void Remove(ProductFavorite favorite);
}