using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Favorite.RemoveFromFavorites;

public sealed class RemoveFromFavoritesCommandHandler : IRequestHandler<RemoveFromFavoritesCommand, ServiceResponse<bool>>
{
    private readonly IProductFavoriteRepository _favoriteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveFromFavoritesCommandHandler> _logger;

    public RemoveFromFavoritesCommandHandler(
        IProductFavoriteRepository favoriteRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveFromFavoritesCommandHandler> _logger)
    {
        _favoriteRepository = favoriteRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        this._logger = _logger;
    }

    public async Task<ServiceResponse<bool>> Handle(RemoveFromFavoritesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing product {ProductId} from favorites for user {UserId}", request.ProductId, request.UserId);

        try
        {
            var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
            if (domainUser is null)
            {
                _logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
                return new ServiceResponse<bool>(false, "User not found");
            }

            var existingFavorite = await _favoriteRepository.GetByUserAndProductAsync(domainUser.Id, request.ProductId);
            if (existingFavorite is null)
            {
                _logger.LogInformation("Product {ProductId} is not in favorites for user {UserId}", request.ProductId, request.UserId);
                return new ServiceResponse<bool>(true, "Product is not in favorites", true);
            }

            _favoriteRepository.Remove(existingFavorite);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} removed from favorites for user {UserId}", request.ProductId, request.UserId);
            return new ServiceResponse<bool>(true, "Product removed from favorites", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from favorites for user {UserId}", request.ProductId, request.UserId);
            return new ServiceResponse<bool>(false, $"Error: {ex.Message}");
        }
    }
}