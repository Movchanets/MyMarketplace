using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Favorite.AddToFavorites;

public sealed record AddToFavoritesCommand(
    Guid UserId,
    Guid ProductId
) : IRequest<ServiceResponse<bool>>, ICacheInvalidatingCommand
{
    public IEnumerable<string> CacheTags => [$"favorites:{UserId}"];
}