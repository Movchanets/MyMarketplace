using System.Text.Json;

namespace Domain.Entities;

public class Product : BaseEntity<Guid>
{
    public Guid? StoreId { get; private set; }
    public virtual Store? Store { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? BaseImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Base attributes for the product. These are inherited by all SKUs
    /// unless overridden at SKU level. Stored as JSONB.
    /// </summary>
    public JsonDocument? Attributes { get; private set; }

    private readonly List<ProductTag> _productTags = new();
    public virtual IReadOnlyCollection<ProductTag> ProductTags => _productTags.AsReadOnly();

    private readonly List<SkuEntity> _skus = new();
    public virtual IReadOnlyCollection<SkuEntity> Skus => _skus.AsReadOnly();

    private readonly List<ProductGallery> _gallery = new();
    public virtual IReadOnlyCollection<ProductGallery> Gallery => _gallery.AsReadOnly();

    private readonly List<ProductCategory> _productCategories = new();
    public virtual IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

    private Product() { }

    public Product(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
        IsActive = true;
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        Name = name.Trim();
        MarkAsUpdated();
    }

    internal void AssignToStore(Store store)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
        StoreId = store.Id;
        MarkAsUpdated();
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }

    public void UpdateBaseImage(string? baseImageUrl)
    {
        BaseImageUrl = string.IsNullOrWhiteSpace(baseImageUrl) ? null : baseImageUrl.Trim();
        MarkAsUpdated();
    }

    public void AddSku(SkuEntity sku)
    {
        if (sku is null) throw new ArgumentNullException(nameof(sku));

        if (_skus.Any(s => s.Id == sku.Id))
        {
            return;
        }

        sku.AttachProduct(this);
        _skus.Add(sku);
    }

    public SkuEntity? RemoveSku(Guid skuId)
    {
        if (skuId == Guid.Empty)
        {
            return null;
        }

        var existing = _skus.FirstOrDefault(s => s.Id == skuId);
        if (existing is null)
        {
            return null;
        }

        _skus.Remove(existing);
        return existing;
    }

    public void AddGalleryItem(MediaImage mediaImage, int displayOrder = 0)
    {
        if (mediaImage is null) throw new ArgumentNullException(nameof(mediaImage));

        var galleryItem = ProductGallery.Create(this, mediaImage, displayOrder);
        _gallery.Add(galleryItem);
    }

    public ProductGallery? RemoveGalleryItem(Guid galleryItemId)
    {
        if (galleryItemId == Guid.Empty)
        {
            return null;
        }

        var existing = _gallery.FirstOrDefault(g => g.Id == galleryItemId);
        if (existing is null)
        {
            return null;
        }

        _gallery.Remove(existing);
        return existing;
    }

    public void AddCategory(Category category)
    {
        if (category is null) throw new ArgumentNullException(nameof(category));

        if (_productCategories.Any(pc => pc.CategoryId == category.Id))
        {
            return;
        }

        var link = ProductCategory.Create(this, category);
        _productCategories.Add(link);
        category.AddProductCategory(link);
    }

    public void RemoveCategory(Guid categoryId)
    {
        var existing = _productCategories.FirstOrDefault(pc => pc.CategoryId == categoryId);
        if (existing is null)
        {
            return;
        }

        existing.Category?.RemoveProductCategory(existing);
        _productCategories.Remove(existing);
    }

    public void AddTag(Tag tag)
    {
        if (tag is null) throw new ArgumentNullException(nameof(tag));

        if (_productTags.Any(pt => pt.TagId == tag.Id))
        {
            return;
        }

        var productTag = ProductTag.Create(this, tag);
        _productTags.Add(productTag);
        tag.AddProductTag(productTag);
    }

    public void RemoveTag(Guid tagId)
    {
        var existing = _productTags.FirstOrDefault(pt => pt.TagId == tagId);
        if (existing is null)
        {
            return;
        }

        existing.Tag?.RemoveProductTag(existing);
        _productTags.Remove(existing);
    }

    /// <summary>
    /// Updates base product attributes. These will be inherited by SKUs.
    /// </summary>
    public void UpdateAttributes(IDictionary<string, object?>? attributes)
    {
        Attributes?.Dispose();

        if (attributes is null || attributes.Count == 0)
        {
            Attributes = null;
        }
        else
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
            var json = JsonSerializer.Serialize(attributes, options);
            Attributes = JsonDocument.Parse(json);
        }
        MarkAsUpdated();
    }

    /// <summary>
    /// Gets attributes as a dictionary.
    /// </summary>
    public Dictionary<string, object?>? GetAttributesDictionary()
    {
        if (Attributes is null) return null;

        try
        {
            var result = new Dictionary<string, object?>();
            foreach (var prop in Attributes.RootElement.EnumerateObject())
            {
                result[prop.Name] = GetJsonElementValue(prop.Value);
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static object? GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(GetJsonElementValue).ToArray(),
            _ => element.GetRawText()
        };
    }
}