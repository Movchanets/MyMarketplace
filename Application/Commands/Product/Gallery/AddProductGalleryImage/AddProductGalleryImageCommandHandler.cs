using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Product.Gallery.AddProductGalleryImage;

public sealed class AddProductGalleryImageCommandHandler : IRequestHandler<AddProductGalleryImageCommand, ServiceResponse<Guid>>
{
	private readonly IProductRepository _productRepository;
	private readonly IMediaImageRepository _mediaImageRepository;
	private readonly IProductGalleryRepository _galleryRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<AddProductGalleryImageCommandHandler> _logger;

	public AddProductGalleryImageCommandHandler(
		IProductRepository productRepository,
		IMediaImageRepository mediaImageRepository,
		IProductGalleryRepository galleryRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<AddProductGalleryImageCommandHandler> logger)
	{
		_productRepository = productRepository;
		_mediaImageRepository = mediaImageRepository;
		_galleryRepository = galleryRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<Guid>> Handle(AddProductGalleryImageCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Adding gallery image {MediaImageId} to product {ProductId} by user {UserId}", request.MediaImageId, request.ProductId, request.UserId);

		try
		{
			// Convert identity user ID to domain user ID
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<Guid>(false, "User not found");
			}

			var product = await _productRepository.GetByIdAsync(request.ProductId);
			if (product?.Store is null || product.Store.UserId != domainUser.Id)
			{
				_logger.LogWarning("Product {ProductId} not found for user {UserId}", request.ProductId, domainUser.Id);
				return new ServiceResponse<Guid>(false, "Product not found");
			}

			if (product.Store.IsSuspended)
			{
				_logger.LogWarning("Store {StoreId} is suspended", product.Store.Id);
				return new ServiceResponse<Guid>(false, "Store is suspended");
			}

			var media = await _mediaImageRepository.GetByIdAsync(request.MediaImageId);
			if (media is null)
			{
				_logger.LogWarning("MediaImage {MediaImageId} not found", request.MediaImageId);
				return new ServiceResponse<Guid>(false, "Image not found");
			}

			var galleryItem = ProductGallery.Create(product, media, request.DisplayOrder);
			_galleryRepository.Add(galleryItem);

			// Якщо ще немає головного зображення — ставимо перше фото з галереї
			if (string.IsNullOrWhiteSpace(product.BaseImageUrl))
			{
				var publicUrl = _galleryRepository.GetPublicUrl(media.StorageKey);
				product.UpdateBaseImage(publicUrl);
			}

			await _unitOfWork.SaveChangesAsync(cancellationToken);

			return new ServiceResponse<Guid>(true, "Gallery image added successfully", galleryItem.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error adding gallery image {MediaImageId} to product {ProductId}", request.MediaImageId, request.ProductId);
			return new ServiceResponse<Guid>(false, $"Error: {ex.Message}");
		}
	}
}
