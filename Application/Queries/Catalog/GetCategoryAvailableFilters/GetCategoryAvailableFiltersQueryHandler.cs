using System.Text.Json;
using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Catalog.GetCategoryAvailableFilters;

public sealed class GetCategoryAvailableFiltersQueryHandler
	: IRequestHandler<GetCategoryAvailableFiltersQuery, ServiceResponse<CategoryAvailableFiltersDto>>
{
	private readonly IProductRepository _productRepository;
	private readonly ICategoryRepository _categoryRepository;
	private readonly IAttributeDefinitionRepository _attributeDefinitionRepository;
	private readonly ILogger<GetCategoryAvailableFiltersQueryHandler> _logger;

	public GetCategoryAvailableFiltersQueryHandler(
		IProductRepository productRepository,
		ICategoryRepository categoryRepository,
		IAttributeDefinitionRepository attributeDefinitionRepository,
		ILogger<GetCategoryAvailableFiltersQueryHandler> logger)
	{
		_productRepository = productRepository;
		_categoryRepository = categoryRepository;
		_attributeDefinitionRepository = attributeDefinitionRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<CategoryAvailableFiltersDto>> Handle(
		GetCategoryAvailableFiltersQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			if (request.CategoryId == Guid.Empty)
			{
				return new ServiceResponse<CategoryAvailableFiltersDto>(false, "CategoryId is required");
			}

			// Get category info
			var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
			if (category is null)
			{
				return new ServiceResponse<CategoryAvailableFiltersDto>(false, "Category not found");
			}

			// Get all active products in category with SKUs
			var products = (await _productRepository.GetActiveByCategoryIdWithSkusAsync(request.CategoryId)).ToList();

			if (products.Count == 0)
			{
				// No products, return empty filters
				return new ServiceResponse<CategoryAvailableFiltersDto>(
					true,
					"No products found in category",
					new CategoryAvailableFiltersDto(
						category.Id,
						category.Name,
						Array.Empty<AttributeFilterDto>(),
						null,
						0
					)
				);
			}

			// Analyze JSONB attributes from all SKUs
			var attributeData = AnalyzeSkuAttributes(products);

			// Get attribute definitions
			var attributeCodes = attributeData.Keys.ToList();
			var attributeDefinitions = (await _attributeDefinitionRepository.GetByCodesAsync(attributeCodes))
				.ToDictionary(ad => ad.Code, StringComparer.OrdinalIgnoreCase);

			// Build filter DTOs
			var filters = new List<AttributeFilterDto>();

			foreach (var (code, data) in attributeData.OrderBy(kvp => 
				attributeDefinitions.ContainsKey(kvp.Key) ? attributeDefinitions[kvp.Key].DisplayOrder : int.MaxValue))
			{
				if (!attributeDefinitions.TryGetValue(code, out var definition))
				{
					// Attribute exists in data but not in definitions - skip or use defaults
					_logger.LogWarning("Attribute code '{Code}' found in products but no AttributeDefinition exists", code);
					continue;
				}

				var filter = definition.DataType.ToLowerInvariant() switch
				{
					"number" => CreateNumberFilter(definition, data.NumberValues),
					"string" or "boolean" => CreateStringFilter(definition, data.StringValues),
					_ => null
				};

				if (filter is not null)
				{
					filters.Add(filter);
				}
			}

			// Calculate price range
			var allPrices = products
				.SelectMany(p => p.Skus)
				.Where(s => s.StockQuantity > 0)
				.Select(s => s.Price)
				.ToList();

			PriceRangeDto? priceRange = allPrices.Count > 0
				? new PriceRangeDto(allPrices.Min(), allPrices.Max())
				: null;

			var result = new CategoryAvailableFiltersDto(
				category.Id,
				category.Name,
				filters.AsReadOnly(),
				priceRange,
				products.Count
			);

			return new ServiceResponse<CategoryAvailableFiltersDto>(
				true,
				"Available filters retrieved successfully",
				result
			);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving available filters for category {CategoryId}", request.CategoryId);
			return new ServiceResponse<CategoryAvailableFiltersDto>(false, $"Error: {ex.Message}");
		}
	}

	/// <summary>
	/// Analyzes all SKU attributes across products and aggregates values by attribute code.
	/// Uses typed SkuAttributeValue entities with fallback to JSONB for backward compatibility.
	/// </summary>
	private Dictionary<string, AttributeAnalysisData> AnalyzeSkuAttributes(List<Domain.Entities.Product> products)
	{
		var attributeData = new Dictionary<string, AttributeAnalysisData>(StringComparer.OrdinalIgnoreCase);

		foreach (var product in products)
		{
			foreach (var sku in product.Skus.Where(s => s.StockQuantity > 0)) // Only in-stock SKUs
			{
				// First, process typed attributes (preferred)
				foreach (var attrValue in sku.AttributeValues)
				{
					var code = attrValue.AttributeDefinition.Code;
					
					if (!attributeData.ContainsKey(code))
					{
						attributeData[code] = new AttributeAnalysisData();
					}

					var data = attributeData[code];
					
					// Add value based on type
					if (attrValue.ValueNumber.HasValue)
					{
						data.NumberValues.Add(attrValue.ValueNumber.Value);
					}
					else if (attrValue.ValueString is not null)
					{
						data.StringValues.Add(attrValue.ValueString);
					}
					else if (attrValue.ValueBoolean.HasValue)
					{
						data.StringValues.Add(attrValue.ValueBoolean.Value.ToString());
					}
				}
				
				// Fallback to JSONB attributes for backward compatibility
				if (sku.Attributes is not null)
				{
					Dictionary<string, JsonElement>? attributes;
					try
					{
						attributes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
							sku.Attributes.RootElement.GetRawText(),
							new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
						);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to parse SKU attributes for SKU {SkuId}", sku.Id);
						continue;
					}

					if (attributes is not null)
					{
						foreach (var (code, value) in attributes)
						{
							// Skip if already processed from typed attributes
							if (sku.AttributeValues.Any(av => 
								string.Equals(av.AttributeDefinition.Code, code, StringComparison.OrdinalIgnoreCase)))
							{
								continue;
							}
							
							if (!attributeData.ContainsKey(code))
							{
								attributeData[code] = new AttributeAnalysisData();
							}

							var data = attributeData[code];

							// Try to parse as number
							if (value.ValueKind == JsonValueKind.Number)
							{
								try
								{
									var numValue = value.GetDecimal();
									data.NumberValues.Add(numValue);
								}
								catch
								{
									// Not a valid number, treat as string
									data.StringValues.Add(value.ToString());
								}
							}
							else if (value.ValueKind == JsonValueKind.String)
							{
								var strValue = value.GetString();
								if (!string.IsNullOrWhiteSpace(strValue))
								{
									data.StringValues.Add(strValue);
								}
							}
							else if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
							{
								data.StringValues.Add(value.GetBoolean().ToString());
							}
						}
					}
				}
			}
		}

		return attributeData;
	}

	private AttributeFilterDto CreateStringFilter(
		Domain.Entities.AttributeDefinition definition,
		List<string> values)
	{
		if (values.Count == 0)
		{
			return null!;
		}

		// Count occurrences of each value
		var valueCounts = values
			.GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
			.Select(g => new AttributeValueOptionDto(g.Key, g.Count()))
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Value)
			.ToList();

		return new AttributeFilterDto(
			definition.Code,
			definition.Name,
			definition.DataType,
			definition.Unit,
			definition.DisplayOrder,
			valueCounts.AsReadOnly(),
			null
		);
	}

	private AttributeFilterDto? CreateNumberFilter(
		Domain.Entities.AttributeDefinition definition,
		List<decimal> values)
	{
		if (values.Count == 0)
		{
			return null;
		}

		var min = values.Min();
		var max = values.Max();

		// Calculate a reasonable step value
		decimal? step = null;
		var range = max - min;
		if (range > 0)
		{
			// Try to find common step from values
			var sortedValues = values.Distinct().OrderBy(v => v).ToList();
			if (sortedValues.Count > 1)
			{
				var differences = new List<decimal>();
				for (int i = 1; i < sortedValues.Count; i++)
				{
					differences.Add(sortedValues[i] - sortedValues[i - 1]);
				}
				
				// Use the most common difference as step
				var commonDiff = differences
					.GroupBy(d => d)
					.OrderByDescending(g => g.Count())
					.FirstOrDefault()?.Key;

				if (commonDiff.HasValue && commonDiff.Value > 0)
				{
					step = commonDiff.Value;
				}
			}
		}

		return new AttributeFilterDto(
			definition.Code,
			definition.Name,
			definition.DataType,
			definition.Unit,
			definition.DisplayOrder,
			null,
			new NumberRangeDto(min, max, step)
		);
	}

	private class AttributeAnalysisData
	{
		public List<string> StringValues { get; } = new();
		public List<decimal> NumberValues { get; } = new();
	}
}
