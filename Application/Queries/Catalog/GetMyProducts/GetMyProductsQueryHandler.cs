using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Catalog.GetMyProducts;

public sealed class GetMyProductsQueryHandler : IRequestHandler<GetMyProductsQuery, ServiceResponse<IReadOnlyList<ProductSummaryDto>>>
{
	private readonly IUserRepository _userRepository;
	private readonly IStoreRepository _storeRepository;
	private readonly IProductRepository _productRepository;
	private readonly ILogger<GetMyProductsQueryHandler> _logger;

	public GetMyProductsQueryHandler(
		IUserRepository userRepository,
		IStoreRepository storeRepository,
		IProductRepository productRepository,
		ILogger<GetMyProductsQueryHandler> logger)
	{
		_userRepository = userRepository;
		_storeRepository = storeRepository;
		_productRepository = productRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<IReadOnlyList<ProductSummaryDto>>> Handle(GetMyProductsQuery request, CancellationToken cancellationToken)
	{
		try
		{
			// Конвертувати IdentityUserId в DomainUserId
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				return new ServiceResponse<IReadOnlyList<ProductSummaryDto>>(
					false,
					"User not found",
					Array.Empty<ProductSummaryDto>().AsReadOnly());
			}

			// Отримати магазин користувача
			var store = await _storeRepository.GetByUserIdAsync(domainUser.Id);
			if (store is null)
			{
				return new ServiceResponse<IReadOnlyList<ProductSummaryDto>>(
					false,
					"Store not found for user",
					Array.Empty<ProductSummaryDto>().AsReadOnly());
			}

			// Отримати продукти магазину
			var products = await _productRepository.GetByStoreIdAsync(store.Id);
			var payload = products
				.Select(ProductMapping.MapSummary)
				.ToList()
				.AsReadOnly();

			return new ServiceResponse<IReadOnlyList<ProductSummaryDto>>(true, "Products retrieved successfully", payload);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving user products for UserId: {UserId}", request.UserId);
			return new ServiceResponse<IReadOnlyList<ProductSummaryDto>>(false, $"Error: {ex.Message}");
		}
	}
}
