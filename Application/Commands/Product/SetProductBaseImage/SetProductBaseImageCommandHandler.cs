using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Product.SetProductBaseImage;

public sealed class SetProductBaseImageCommandHandler : IRequestHandler<SetProductBaseImageCommand, ServiceResponse>
{
	private readonly IProductRepository _productRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<SetProductBaseImageCommandHandler> _logger;

	public SetProductBaseImageCommandHandler(
		IProductRepository productRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<SetProductBaseImageCommandHandler> logger)
	{
		_productRepository = productRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(SetProductBaseImageCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Setting base image for product {ProductId} by user {UserId}", request.ProductId, request.UserId);

		try
		{
			// Convert identity user ID to domain user ID
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse(false, "User not found");
			}

			var product = await _productRepository.GetByIdAsync(request.ProductId);
			if (product?.Store is null || product.Store.UserId != domainUser.Id)
			{
				_logger.LogWarning("Product {ProductId} not found for user {UserId}", request.ProductId, domainUser.Id);
				return new ServiceResponse(false, "Product not found");
			}

			if (product.Store.IsSuspended)
			{
				_logger.LogWarning("Store {StoreId} is suspended", product.Store.Id);
				return new ServiceResponse(false, "Store is suspended");
			}

			product.UpdateBaseImage(request.BaseImageUrl);
			await _unitOfWork.SaveChangesAsync(cancellationToken);
			return new ServiceResponse(true, "Product base image updated successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error setting base image for product {ProductId}", request.ProductId);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
