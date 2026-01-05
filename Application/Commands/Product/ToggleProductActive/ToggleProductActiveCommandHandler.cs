using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Product.ToggleProductActive;

public sealed class ToggleProductActiveCommandHandler : IRequestHandler<ToggleProductActiveCommand, ServiceResponse>
{
	private readonly IProductRepository _productRepository;
	private readonly IStoreRepository _storeRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<ToggleProductActiveCommandHandler> _logger;

	public ToggleProductActiveCommandHandler(
		IProductRepository productRepository,
		IStoreRepository storeRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<ToggleProductActiveCommandHandler> logger)
	{
		_productRepository = productRepository;
		_storeRepository = storeRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(ToggleProductActiveCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Toggling product {ProductId} active status to {IsActive} for user {UserId}",
			request.ProductId, request.IsActive, request.UserId);

		// Get domain user from identity user id
		var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
		if (domainUser == null)
		{
			_logger.LogWarning("Domain user not found for identity user {UserId}", request.UserId);
			return new ServiceResponse(false, "User not found");
		}

		// Verify user owns the store
		var store = await _storeRepository.GetByUserIdAsync(domainUser.Id);
		if (store == null)
		{
			_logger.LogWarning("Store not found for user {UserId}", request.UserId);
			return new ServiceResponse(false, "Store not found for user");
		}

		// Get product
		var product = await _productRepository.GetByIdAsync(request.ProductId);
		if (product == null)
		{
			_logger.LogWarning("Product {ProductId} not found", request.ProductId);
			return new ServiceResponse(false, "Product not found");
		}

		// Verify product belongs to user's store
		if (product.StoreId != store.Id)
		{
			_logger.LogWarning("Product {ProductId} does not belong to store {StoreId}", request.ProductId, store.Id);
			return new ServiceResponse(false, "Product does not belong to your store");
		}

		// Toggle active status
		if (request.IsActive)
		{
			product.Activate();
		}
		else
		{
			product.Deactivate();
		}

		await _unitOfWork.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Product {ProductId} active status set to {IsActive}", request.ProductId, request.IsActive);

		return new ServiceResponse(true, request.IsActive ? "Product activated" : "Product deactivated");
	}
}
