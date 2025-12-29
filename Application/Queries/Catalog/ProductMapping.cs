using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Queries.Catalog;

internal static class ProductMapping
{
	public static ProductSummaryDto MapSummary(Product product)
	{
		var categories = product.ProductCategories
			.Where(pc => pc.Category is not null)
			.Select(pc => pc.Category!)
			.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ParentCategoryId))
			.ToList()
			.AsReadOnly();

		var tags = product.ProductTags
			.Where(pt => pt.Tag is not null)
			.Select(pt => pt.Tag!)
			.Select(t => new TagDto(t.Id, t.Name, t.Slug, t.Description))
			.ToList()
			.AsReadOnly();

		var minPrice = product.Skus.Count == 0 ? (decimal?)null : product.Skus.Min(s => s.Price);
		var inStock = product.Skus.Any(s => s.StockQuantity > 0);

		return new ProductSummaryDto(
			product.Id,
			product.StoreId,
			product.Name,
			product.BaseImageUrl,
			minPrice,
			inStock,
			product.IsActive,
			categories,
			tags);
	}

	public static ProductDetailsDto MapDetails(Product product, IFileStorage fileStorage)
	{
		var productAttributes = product.GetAttributesDictionary();

		var skus = product.Skus
			.Select(s => MapSku(s, productAttributes))
			.ToList()
			.AsReadOnly();

		var gallery = product.Gallery
			.OrderBy(g => g.DisplayOrder)
			.Select(g => MapGalleryImage(g, fileStorage))
			.Where(dto => dto is not null)
			.Cast<MediaImageDto>()
			.ToList()
			.AsReadOnly();

		var categories = product.ProductCategories
			.Where(pc => pc.Category is not null)
			.Select(pc => pc.Category!)
			.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ParentCategoryId))
			.ToList()
			.AsReadOnly();

		var tags = product.ProductTags
			.Where(pt => pt.Tag is not null)
			.Select(pt => pt.Tag!)
			.Select(t => new TagDto(t.Id, t.Name, t.Slug, t.Description))
			.ToList()
			.AsReadOnly();

		return new ProductDetailsDto(
			product.Id,
			product.StoreId,
			product.Name,
			product.Description,
			product.BaseImageUrl,
			productAttributes,
			skus,
			gallery,
			categories,
			tags);
	}

	/// <summary>
	/// Maps SKU entity to DTO, merging product-level attributes with SKU-level attributes.
	/// SKU attributes override product attributes with the same key.
	/// </summary>
	private static SkuDto MapSku(SkuEntity sku, Dictionary<string, object?>? productAttributes)
	{
		var skuAttributes = CatalogDtoJson.AttributesToDictionary(sku.Attributes);
		var mergedAttributes = MergeAttributes(productAttributes, skuAttributes);

		return new SkuDto(
			sku.Id,
			sku.SkuCode,
			sku.Price,
			sku.StockQuantity,
			skuAttributes,
			mergedAttributes
		);
	}

	/// <summary>
	/// Merges product-level and SKU-level attributes.
	/// SKU attributes take precedence over product attributes.
	/// </summary>
	private static Dictionary<string, object?>? MergeAttributes(
		Dictionary<string, object?>? productAttributes,
		Dictionary<string, object?>? skuAttributes)
	{
		// If both are null or empty, return null
		if ((productAttributes is null || productAttributes.Count == 0) &&
			(skuAttributes is null || skuAttributes.Count == 0))
		{
			return null;
		}

		// Start with product attributes (base)
		var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

		if (productAttributes is not null)
		{
			foreach (var kvp in productAttributes)
			{
				merged[kvp.Key] = kvp.Value;
			}
		}

		// Override with SKU attributes (variant-specific)
		if (skuAttributes is not null)
		{
			foreach (var kvp in skuAttributes)
			{
				merged[kvp.Key] = kvp.Value;
			}
		}

		return merged.Count > 0 ? merged : null;
	}

	private static MediaImageDto? MapGalleryImage(ProductGallery galleryItem, IFileStorage fileStorage)
	{
		var media = galleryItem.MediaImage;
		if (media is null)
		{
			return null;
		}

		var url = fileStorage.GetPublicUrl(media.StorageKey);
		return new MediaImageDto(
			media.Id,
			media.StorageKey,
			url,
			media.MimeType,
			media.Width,
			media.Height,
			media.AltText);
	}
}
