using Application.DTOs;
using Application.Queries.Catalog;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Store.GetStoreBySlug;

public sealed class GetStoreBySlugQueryHandler : IRequestHandler<GetStoreBySlugQuery, ServiceResponse<PublicStoreDto?>>
{
	private readonly IStoreRepository _storeRepository;
	private readonly ILogger<GetStoreBySlugQueryHandler> _logger;

	public GetStoreBySlugQueryHandler(
		IStoreRepository storeRepository,
		ILogger<GetStoreBySlugQueryHandler> logger)
	{
		_storeRepository = storeRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<PublicStoreDto?>> Handle(GetStoreBySlugQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Getting store by slug: {Slug}", request.Slug);

		try
		{
			var store = await _storeRepository.GetBySlugAsync(request.Slug);

			if (store is null)
			{
				_logger.LogWarning("Store not found for slug: {Slug}", request.Slug);
				return new ServiceResponse<PublicStoreDto?>(false, "Store not found", null);
			}

			// Only show verified stores publicly
			if (!store.IsVerified)
			{
				_logger.LogWarning("Store {Slug} is not verified", request.Slug);
				return new ServiceResponse<PublicStoreDto?>(false, "Store not found", null);
			}

			// Only show active products
			var activeProducts = store.Products
				.Where(p => p.IsActive)
				.Select(ProductMapping.MapSummary)
				.ToList()
				.AsReadOnly();

			var dto = new PublicStoreDto(
				store.Id,
				store.Name,
				store.Slug,
				store.Description,
				store.IsVerified,
				store.CreatedAt,
				activeProducts
			);

			_logger.LogInformation("Store {Slug} found with {ProductCount} active products", request.Slug, activeProducts.Count);
			return new ServiceResponse<PublicStoreDto?>(true, "Store retrieved successfully", dto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting store by slug: {Slug}", request.Slug);
			return new ServiceResponse<PublicStoreDto?>(false, $"Error: {ex.Message}");
		}
	}
}
