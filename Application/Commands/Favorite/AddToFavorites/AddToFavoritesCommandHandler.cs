using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Favorite.AddToFavorites;

public sealed class AddToFavoritesCommandHandler : IRequestHandler<AddToFavoritesCommand, ServiceResponse<bool>>
{
    private readonly IProductFavoriteRepository _favoriteRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddToFavoritesCommandHandler> _logger;

    public AddToFavoritesCommandHandler(
        IProductFavoriteRepository favoriteRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddToFavoritesCommandHandler> _logger)
    {
        _favoriteRepository = favoriteRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        this._logger = _logger;
    }

    public async Task<ServiceResponse<bool>> Handle(AddToFavoritesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding product {ProductId} to favorites for user {UserId}", request.ProductId, request.UserId);

        try
        {
            var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
            if (domainUser is null)
            {
                _logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
                return new ServiceResponse<bool>(false, "User not found");
            }

            var product = await _productRepository.GetByIdAsync(request.ProductId);
            if (product is null)
            {
                _logger.LogWarning("Product {ProductId} not found", request.ProductId);
                return new ServiceResponse<bool>(false, "Product not found");
            }

            if (!product.IsActive)
            {
                _logger.LogWarning("Product {ProductId} is not active", request.ProductId);
                return new ServiceResponse<bool>(false, "Product is not available");
            }

            var exists = await _favoriteRepository.ExistsAsync(domainUser.Id, request.ProductId);
            if (exists)
            {
                _logger.LogInformation("Product {ProductId} is already in favorites for user {UserId}", request.ProductId, request.UserId);
                return new ServiceResponse<bool>(true, "Product is already in favorites", true);
            }

            var favorite = new ProductFavorite
            {
                UserId = domainUser.Id,
                ProductId = request.ProductId,
                User = domainUser,
                Product = product
            };

            _favoriteRepository.Add(favorite);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} added to favorites for user {UserId}", request.ProductId, request.UserId);
            return new ServiceResponse<bool>(true, "Product added to favorites", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductId} to favorites for user {UserId}", request.ProductId, request.UserId);
            return new ServiceResponse<bool>(false, $"Error: {ex.Message}");
        }
    }
}