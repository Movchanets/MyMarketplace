using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Favorite.RemoveFromFavorites;

public sealed record RemoveFromFavoritesCommand(
    Guid UserId,
    Guid ProductId
) : IRequest<ServiceResponse<bool>>, ICacheInvalidatingCommand
{
    public IEnumerable<string> CacheTags => [$"favorites:{UserId}"];
}