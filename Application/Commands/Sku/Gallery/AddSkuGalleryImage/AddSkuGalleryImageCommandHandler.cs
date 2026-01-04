using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Sku.Gallery.AddSkuGalleryImage;

public sealed class AddSkuGalleryImageCommandHandler : IRequestHandler<AddSkuGalleryImageCommand, ServiceResponse<Guid>>
{
	private readonly ISkuRepository _skuRepository;
	private readonly IMediaImageRepository _mediaImageRepository;
	private readonly ISkuGalleryRepository _galleryRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<AddSkuGalleryImageCommandHandler> _logger;

	public AddSkuGalleryImageCommandHandler(
		ISkuRepository skuRepository,
		IMediaImageRepository mediaImageRepository,
		ISkuGalleryRepository galleryRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<AddSkuGalleryImageCommandHandler> logger)
	{
		_skuRepository = skuRepository;
		_mediaImageRepository = mediaImageRepository;
		_galleryRepository = galleryRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<Guid>> Handle(AddSkuGalleryImageCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Adding gallery image {MediaImageId} to SKU {SkuId} by user {UserId}",
			request.MediaImageId, request.SkuId, request.UserId);

		try
		{
			// Convert identity user ID to domain user ID
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<Guid>(false, "User not found");
			}

			var sku = await _skuRepository.GetByIdAsync(request.SkuId);
			if (sku?.Product?.Store is null || sku.ProductId != request.ProductId || sku.Product.Store.UserId != domainUser.Id)
			{
				_logger.LogWarning("SKU {SkuId} not found for product {ProductId} and user {UserId}",
					request.SkuId, request.ProductId, domainUser.Id);
				return new ServiceResponse<Guid>(false, "SKU not found");
			}

			if (sku.Product.Store.IsSuspended)
			{
				_logger.LogWarning("Store {StoreId} is suspended", sku.Product.Store.Id);
				return new ServiceResponse<Guid>(false, "Store is suspended");
			}

			var media = await _mediaImageRepository.GetByIdAsync(request.MediaImageId);
			if (media is null)
			{
				_logger.LogWarning("MediaImage {MediaImageId} not found", request.MediaImageId);
				return new ServiceResponse<Guid>(false, "Image not found");
			}

			var galleryItem = SkuGallery.Create(sku, media, request.DisplayOrder);
			_galleryRepository.Add(galleryItem);

			await _unitOfWork.SaveChangesAsync(cancellationToken);

			return new ServiceResponse<Guid>(true, "SKU gallery image added successfully", galleryItem.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error adding gallery image {MediaImageId} to SKU {SkuId}", request.MediaImageId, request.SkuId);
			return new ServiceResponse<Guid>(false, $"Error: {ex.Message}");
		}
	}
}
