using System.Text.Json;

namespace Application.DTOs;

public sealed record IdResponse(Guid Id);

public sealed record PagedResponse<T>(
	IReadOnlyList<T> Items,
	int Page,
	int PageSize,
	int Total
);

public sealed record StoreSummaryDto(
	Guid Id,
	string Name,
	string Slug,
	string? Description,
	bool IsVerified
);

public sealed record StoreDetailsDto(
	Guid Id,
	string Name,
	string Slug,
	string? Description,
	bool IsVerified,
	bool IsSuspended
);

public sealed record CategoryDto(
	Guid Id,
	string Name,
	string Slug,
	string? Description,
	Guid? ParentCategoryId
);

public sealed record TagDto(
	Guid Id,
	string Name,
	string Slug,
	string? Description
);

public sealed record SkuDto(
	Guid Id,
	string SkuCode,
	decimal Price,
	int StockQuantity,
	Dictionary<string, object?>? Attributes
);

public sealed record MediaImageDto(
	Guid Id,
	string StorageKey,
	string Url,
	string MimeType,
	int Width,
	int Height,
	string? AltText
);

public sealed record ProductSummaryDto(
	Guid Id,
	Guid? StoreId,
	string Name,
	string? BaseImageUrl,
	decimal? MinPrice,
	bool InStock,
	bool IsActive,
	IReadOnlyList<CategoryDto> Categories,
	IReadOnlyList<TagDto> Tags
);

public sealed record ProductDetailsDto(
	Guid Id,
	Guid? StoreId,
	string Name,
	string? Description,
	string? BaseImageUrl,
	IReadOnlyList<SkuDto> Skus,
	IReadOnlyList<MediaImageDto> Gallery,
	IReadOnlyList<CategoryDto> Categories,
	IReadOnlyList<TagDto> Tags
);

public enum ProductSort
{
	Relevance = 0,
	Newest = 1,
	PriceAsc = 2,
	PriceDesc = 3
}

public sealed record ProductSearchRequest(
	string? Query,
	List<Guid>? CategoryIds,
	List<Guid>? TagIds,
	decimal? PriceFrom,
	decimal? PriceTo,
	bool? InStock,
	Dictionary<string, string?>? SkuAttributes,
	ProductSort Sort = ProductSort.Relevance,
	int Page = 1,
	int PageSize = 24
);

public static class CatalogDtoJson
{
	public static Dictionary<string, object?>? AttributesToDictionary(JsonDocument? attributes)
	{
		if (attributes is null)
		{
			return null;
		}

		var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(attributes.RootElement.GetRawText());
		return dictionary;
	}
}
