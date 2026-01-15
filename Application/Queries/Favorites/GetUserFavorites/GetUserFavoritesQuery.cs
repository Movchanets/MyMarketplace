using Application.DTOs;
using MediatR;

namespace Application.Queries.Favorites.GetUserFavorites;

public sealed record GetUserFavoritesQuery(Guid UserId) : IRequest<ServiceResponse<IReadOnlyList<FavoriteProductDto>>>;