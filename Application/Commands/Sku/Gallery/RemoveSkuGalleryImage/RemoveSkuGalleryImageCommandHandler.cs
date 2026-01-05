using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Sku.Gallery.RemoveSkuGalleryImage;

public sealed class RemoveSkuGalleryImageCommandHandler : IRequestHandler<RemoveSkuGalleryImageCommand, ServiceResponse>
{
	private readonly ISkuRepository _skuRepository;
	private readonly ISkuGalleryRepository _galleryRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<RemoveSkuGalleryImageCommandHandler> _logger;

	public RemoveSkuGalleryImageCommandHandler(
		ISkuRepository skuRepository,
		ISkuGalleryRepository galleryRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<RemoveSkuGalleryImageCommandHandler> logger)
	{
		_skuRepository = skuRepository;
		_galleryRepository = galleryRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(RemoveSkuGalleryImageCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Removing gallery image {GalleryItemId} from SKU {SkuId} by user {UserId}",
			request.GalleryItemId, request.SkuId, request.UserId);

		try
		{
			// Convert identity user ID to domain user ID
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse(false, "User not found");
			}

			var sku = await _skuRepository.GetByIdAsync(request.SkuId);
			if (sku?.Product?.Store is null || sku.ProductId != request.ProductId || sku.Product.Store.UserId != domainUser.Id)
			{
				_logger.LogWarning("SKU {SkuId} not found for product {ProductId} and user {UserId}",
					request.SkuId, request.ProductId, domainUser.Id);
				return new ServiceResponse(false, "SKU not found");
			}

			if (sku.Product.Store.IsSuspended)
			{
				_logger.LogWarning("Store {StoreId} is suspended", sku.Product.Store.Id);
				return new ServiceResponse(false, "Store is suspended");
			}

			var galleryItem = await _galleryRepository.GetByIdAsync(request.GalleryItemId);
			if (galleryItem is null || galleryItem.SkuId != sku.Id)
			{
				_logger.LogWarning("Gallery item {GalleryItemId} not found for SKU {SkuId}", request.GalleryItemId, request.SkuId);
				return new ServiceResponse(false, "Gallery item not found");
			}

			await _galleryRepository.DeleteWithFileAsync(galleryItem);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			return new ServiceResponse(true, "SKU gallery image removed successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error removing gallery image {GalleryItemId} from SKU {SkuId}", request.GalleryItemId, request.SkuId);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
