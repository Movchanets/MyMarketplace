namespace Domain.Entities;

/// <summary>
/// Gallery images for a specific SKU variant.
/// Used for visual variants (color, material) that need their own photos.
/// </summary>
public class SkuGallery : BaseEntity<Guid>
{
	public Guid SkuId { get; private set; }
	public virtual SkuEntity? Sku { get; private set; }
	public Guid MediaImageId { get; private set; }
	public virtual MediaImage? MediaImage { get; private set; }
	public int DisplayOrder { get; private set; }

	private SkuGallery() { }

	private SkuGallery(Guid skuId, Guid mediaImageId, int displayOrder)
	{
		if (skuId == Guid.Empty)
		{
			throw new ArgumentException("SkuId cannot be empty", nameof(skuId));
		}

		if (mediaImageId == Guid.Empty)
		{
			throw new ArgumentException("MediaImageId cannot be empty", nameof(mediaImageId));
		}

		if (displayOrder < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(displayOrder), "DisplayOrder cannot be negative");
		}

		Id = Guid.NewGuid();
		SkuId = skuId;
		MediaImageId = mediaImageId;
		DisplayOrder = displayOrder;
	}

	public static SkuGallery Create(SkuEntity sku, MediaImage mediaImage, int displayOrder = 0)
	{
		if (sku is null)
		{
			throw new ArgumentNullException(nameof(sku));
		}

		if (mediaImage is null)
		{
			throw new ArgumentNullException(nameof(mediaImage));
		}

		var galleryItem = new SkuGallery(sku.Id, mediaImage.Id, displayOrder);
		galleryItem.Attach(sku, mediaImage);
		return galleryItem;
	}

	public void SetDisplayOrder(int displayOrder)
	{
		if (displayOrder < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(displayOrder), "DisplayOrder cannot be negative");
		}

		DisplayOrder = displayOrder;
		MarkAsUpdated();
	}

	internal void Attach(SkuEntity sku, MediaImage mediaImage)
	{
		Sku = sku ?? throw new ArgumentNullException(nameof(sku));
		MediaImage = mediaImage ?? throw new ArgumentNullException(nameof(mediaImage));
	}
}
