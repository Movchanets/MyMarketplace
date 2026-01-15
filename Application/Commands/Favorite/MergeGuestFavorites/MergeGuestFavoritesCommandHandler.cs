using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Favorite.MergeGuestFavorites;

public sealed class MergeGuestFavoritesCommandHandler : IRequestHandler<MergeGuestFavoritesCommand, ServiceResponse<int>>
{
    private readonly IProductFavoriteRepository _favoriteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MergeGuestFavoritesCommandHandler> _logger;

    public MergeGuestFavoritesCommandHandler(
        IProductFavoriteRepository favoriteRepository,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<MergeGuestFavoritesCommandHandler> _logger)
    {
        _favoriteRepository = favoriteRepository;
        _userRepository = userRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        this._logger = _logger;
    }

    public async Task<ServiceResponse<int>> Handle(MergeGuestFavoritesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Merging {Count} guest favorites for user {UserId}", request.ProductIds.Count(), request.UserId);

        try
        {
            var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
            if (domainUser is null)
            {
                _logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
                return new ServiceResponse<int>(false, "User not found");
            }

            var mergedCount = 0;
            foreach (var productId in request.ProductIds.Distinct())
            {
                // Check if product exists and is active
                var product = await _productRepository.GetByIdAsync(productId);
                if (product is null || !product.IsActive)
                {
                    _logger.LogWarning("Product {ProductId} not found or not active, skipping", productId);
                    continue;
                }

                // Check if already in favorites
                var exists = await _favoriteRepository.ExistsAsync(domainUser.Id, productId);
                if (exists)
                {
                    _logger.LogDebug("Product {ProductId} already in favorites for user {UserId}", productId, request.UserId);
                    continue;
                }

                // Add to favorites
                var favorite = new ProductFavorite
                {
                    UserId = domainUser.Id,
                    ProductId = productId,
                    User = domainUser,
                    Product = product
                };

                _favoriteRepository.Add(favorite);
                mergedCount++;
            }

            if (mergedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Successfully merged {MergedCount} guest favorites for user {UserId}", mergedCount, request.UserId);
            return new ServiceResponse<int>(true, $"Merged {mergedCount} favorites", mergedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging guest favorites for user {UserId}", request.UserId);
            return new ServiceResponse<int>(false, $"Error: {ex.Message}");
        }
    }
}