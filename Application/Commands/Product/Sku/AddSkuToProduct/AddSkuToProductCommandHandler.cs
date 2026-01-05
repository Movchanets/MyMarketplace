using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Product.Sku.AddSkuToProduct;

public sealed class AddSkuToProductCommandHandler : IRequestHandler<AddSkuToProductCommand, ServiceResponse<string>>
{
	private readonly IProductRepository _productRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<AddSkuToProductCommandHandler> _logger;

	public AddSkuToProductCommandHandler(
		IProductRepository productRepository,
		ISkuRepository skuRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<AddSkuToProductCommandHandler> logger)
	{
		_productRepository = productRepository;
		_skuRepository = skuRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<string>> Handle(AddSkuToProductCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Adding SKU to product {ProductId} by user {UserId}", request.ProductId, request.UserId);

		try
		{
			// Convert identity user ID to domain user ID
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<string>(false, "User not found");
			}

			var product = await _productRepository.GetByIdAsync(request.ProductId);
			if (product?.Store is null || product.Store.UserId != domainUser.Id)
			{
				_logger.LogWarning("Product {ProductId} not found for user {UserId}", request.ProductId, domainUser.Id);
				return new ServiceResponse<string>(false, "Product not found");
			}

			if (product.Store.IsSuspended)
			{
				_logger.LogWarning("Store {StoreId} is suspended", product.Store.Id);
				return new ServiceResponse<string>(false, "Store is suspended");
			}

			var sku = SkuEntity.Create(product.Id, request.Price, request.StockQuantity, request.Attributes);
			_skuRepository.Add(sku);

			await _unitOfWork.SaveChangesAsync(cancellationToken);
			return new ServiceResponse<string>(true, "SKU added successfully", sku.SkuCode);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error adding SKU to product {ProductId}", request.ProductId);
			return new ServiceResponse<string>(false, $"Error: {ex.Message}");
		}
	}
}
