using Application.DTOs;
using Application.Queries.Favorites.GetUserFavorites;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Queries.Favorites.GetUserFavorites;

public class GetUserFavoritesQueryHandler : IRequestHandler<GetUserFavoritesQuery, ServiceResponse<IReadOnlyList<FavoriteProductDto>>>
{
    private readonly IProductFavoriteRepository _favoriteRepository;

    public GetUserFavoritesQueryHandler(IProductFavoriteRepository favoriteRepository)
    {
        _favoriteRepository = favoriteRepository;
    }

    public async Task<ServiceResponse<IReadOnlyList<FavoriteProductDto>>> Handle(
        GetUserFavoritesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var favorites = await _favoriteRepository.GetByUserIdAsync(request.UserId);

            var favoriteDtos = favorites.Select(f => new FavoriteProductDto(
                f.Product.Id,
                f.Product.Name,
                f.Product.Slug,
                f.Product.BaseImageUrl,
                f.Product.Skus.FirstOrDefault()?.Price,
                f.Product.Skus.Any(s => s.StockQuantity > 0),
                f.CreatedAt
            )).ToList().AsReadOnly();

            return new ServiceResponse<IReadOnlyList<FavoriteProductDto>>(true, "Favorites retrieved successfully", favoriteDtos);
        }
        catch (Exception ex)
        {
            return new ServiceResponse<IReadOnlyList<FavoriteProductDto>>(false, $"Error retrieving favorites: {ex.Message}");
        }
    }
}