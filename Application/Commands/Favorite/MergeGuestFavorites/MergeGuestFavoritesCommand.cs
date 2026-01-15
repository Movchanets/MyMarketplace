using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Favorite.MergeGuestFavorites;

public sealed record MergeGuestFavoritesCommand(
    Guid UserId,
    IEnumerable<Guid> ProductIds
) : IRequest<ServiceResponse<int>>, ICacheInvalidatingCommand
{
    public IEnumerable<string> CacheTags => [$"favorites:{UserId}"];
}