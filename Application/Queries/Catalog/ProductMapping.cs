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
		var skus = product.Skus
			.Select(s => new SkuDto(
				s.Id,
				s.SkuCode,
				s.Price,
				s.StockQuantity,
				CatalogDtoJson.AttributesToDictionary(s.Attributes)))
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
			skus,
			gallery,
			categories,
			tags);
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
