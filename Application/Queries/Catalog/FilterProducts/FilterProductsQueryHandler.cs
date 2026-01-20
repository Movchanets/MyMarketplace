using Application.DTOs;
using Application.Queries.Catalog;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Catalog.FilterProducts;

public sealed class FilterProductsQueryHandler
	: IRequestHandler<FilterProductsQuery, ServiceResponse<PagedResponse<ProductSummaryDto>>>
{
	private readonly IProductRepository _productRepository;
	private readonly ILogger<FilterProductsQueryHandler> _logger;

	public FilterProductsQueryHandler(
		IProductRepository productRepository,
		ILogger<FilterProductsQueryHandler> logger)
	{
		_productRepository = productRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<PagedResponse<ProductSummaryDto>>> Handle(
		FilterProductsQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("FilterProducts request: {@Request}", request);
			
			// Validate pagination
			if (request.Page < 1) return new ServiceResponse<PagedResponse<ProductSummaryDto>>(false, "Page must be >= 1");
			if (request.PageSize < 1 || request.PageSize > 100) 
				return new ServiceResponse<PagedResponse<ProductSummaryDto>>(false, "PageSize must be between 1 and 100");

			// Convert AttributeFilterValue to dictionary format for repository
			Dictionary<string, object>? attributeFilters = null;
			if (request.Attributes is not null && request.Attributes.Count > 0)
			{
				attributeFilters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				foreach (var (code, filterValue) in request.Attributes)
				{
					var filterDict = new Dictionary<string, object>();

					if (filterValue.In is not null && filterValue.In.Count > 0)
					{
						filterDict["In"] = filterValue.In;
					}
					if (filterValue.Equal is not null)
					{
						filterDict["Equal"] = filterValue.Equal;
					}
					if (filterValue.Gte.HasValue)
					{
						filterDict["Gte"] = filterValue.Gte.Value;
					}
					if (filterValue.Lte.HasValue)
					{
						filterDict["Lte"] = filterValue.Lte.Value;
					}
					if (filterValue.Eq.HasValue)
					{
						filterDict["Eq"] = filterValue.Eq.Value;
					}

					if (filterDict.Count > 0)
					{
						attributeFilters[code] = filterDict;
					}
				}
			}

			// Call repository filter method
			var (products, totalCount) = await _productRepository.FilterAsync(
				request.CategoryId,
				request.TagIds,
				request.MinPrice,
				request.MaxPrice,
				request.InStock,
				attributeFilters,
				request.Sort.ToString(),
				request.Page,
				request.PageSize
			);

			// Map to DTOs
			var productDtos = products
				.Select(ProductMapping.MapSummary)
				.ToList()
				.AsReadOnly();

			var response = new PagedResponse<ProductSummaryDto>(
				productDtos,
				request.Page,
				request.PageSize,
				totalCount
			);

			return new ServiceResponse<PagedResponse<ProductSummaryDto>>(
				true,
				"Products filtered successfully",
				response
			);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error filtering products with request {@Request}", request);
			return new ServiceResponse<PagedResponse<ProductSummaryDto>>(false, $"Error: {ex.Message}");
		}
	}
}
