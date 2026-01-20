namespace Application.DTOs;

/// <summary>
/// Response containing all available filter options for products in a category.
/// Analyzes actual product data to return only filters with real values.
/// </summary>
public sealed record CategoryAvailableFiltersDto(
	Guid CategoryId,
	string CategoryName,
	IReadOnlyList<AttributeFilterDto> Attributes,
	PriceRangeDto? PriceRange,
	int TotalProductCount
);

/// <summary>
/// Describes a single attribute filter with available values or numeric range.
/// </summary>
public sealed record AttributeFilterDto(
	string Code,
	string Name,
	string DataType,
	string? Unit,
	int DisplayOrder,
	IReadOnlyList<AttributeValueOptionDto>? AvailableValues,
	NumberRangeDto? NumberRange
);

/// <summary>
/// A single available value for a string/boolean attribute with product count.
/// </summary>
public sealed record AttributeValueOptionDto(
	string Value,
	int Count
);

/// <summary>
/// Min/max range for numeric attributes.
/// </summary>
public sealed record NumberRangeDto(
	decimal Min,
	decimal Max,
	decimal? Step = null
);

/// <summary>
/// Price range across all products in category.
/// </summary>
public sealed record PriceRangeDto(
	decimal Min,
	decimal Max
);
