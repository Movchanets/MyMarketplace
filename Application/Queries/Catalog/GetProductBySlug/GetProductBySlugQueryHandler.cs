using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Catalog.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, ServiceResponse<ProductDetailsDto>>
{
	private readonly IProductRepository _productRepository;
	private readonly IFileStorage _fileStorage;
	private readonly ILogger<GetProductBySlugQueryHandler> _logger;

	public GetProductBySlugQueryHandler(
		IProductRepository productRepository,
		IFileStorage fileStorage,
		ILogger<GetProductBySlugQueryHandler> logger)
	{
		_productRepository = productRepository;
		_fileStorage = fileStorage;
		_logger = logger;
	}

	public async Task<ServiceResponse<ProductDetailsDto>> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
	{
		try
		{
			var product = await _productRepository.GetBySlugAsync(request.ProductSlug);
			if (product is null)
			{
				return new ServiceResponse<ProductDetailsDto>(false, "Product not found");
			}

			var dto = ProductMapping.MapDetails(product, _fileStorage);

			// If SkuCode provided, reorder SKUs to put matching one first (for default selection)
			if (!string.IsNullOrWhiteSpace(request.SkuCode) && dto.Skus.Count > 0)
			{
				var matchingSku = dto.Skus.FirstOrDefault(s =>
					s.SkuCode.Equals(request.SkuCode, StringComparison.OrdinalIgnoreCase));

				if (matchingSku != null)
				{
					// Move matching SKU to first position for frontend convenience
					var reorderedSkus = new List<SkuDto> { matchingSku };
					reorderedSkus.AddRange(dto.Skus.Where(s => s.Id != matchingSku.Id));
					dto = dto with { Skus = reorderedSkus };
				}
			}

			return new ServiceResponse<ProductDetailsDto>(true, "Product retrieved successfully", dto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving product by slug {ProductSlug}", request.ProductSlug);
			return new ServiceResponse<ProductDetailsDto>(false, $"Error: {ex.Message}");
		}
	}
}
