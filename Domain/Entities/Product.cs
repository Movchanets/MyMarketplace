using System.Text.Json;
using Domain.Helpers;

namespace Domain.Entities;

public class Product : BaseEntity<Guid>
{
    public Guid? StoreId { get; private set; }
    public virtual Store? Store { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
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
        Slug = GenerateSlug(name);
        Description = description?.Trim();
        IsActive = true;
    }

    /// <summary>
    /// Generates a unique slug for the product based on name.
    /// Appends a short hash suffix to ensure uniqueness.
    /// </summary>
    private string GenerateSlug(string name)
    {
        var baseSlug = SlugHelper.GenerateSlug(name);
        var shortId = Id.ToString("N")[..6]; // First 6 chars of GUID
        return $"{baseSlug}-{shortId}";
    }

    /// <summary>
    /// Regenerates the slug. Use when name changes or to ensure uniqueness.
    /// </summary>
    public void RegenerateSlug()
    {
        Slug = GenerateSlug(Name);
        MarkAsUpdated();
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
        Slug = GenerateSlug(name);
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

    public void AddCategory(Category category, bool isPrimary = false)
    {
        if (category is null) throw new ArgumentNullException(nameof(category));

        if (_productCategories.Any(pc => pc.CategoryId == category.Id))
        {
            return;
        }

        // If this is the first category or explicitly marked as primary, set it
        var shouldBePrimary = isPrimary || !_productCategories.Any();

        // If setting as primary, remove primary from existing categories
        if (shouldBePrimary)
        {
            foreach (var pc in _productCategories.Where(pc => pc.IsPrimary))
            {
                pc.RemovePrimary();
            }
        }

        var link = ProductCategory.Create(this, category, shouldBePrimary);
        _productCategories.Add(link);
        // Note: EF Core handles the relationship automatically via navigation properties
        // No need to call category.AddProductCategory(link) - it causes tracking conflicts
    }

    /// <summary>
    /// Adds categories by their IDs without loading Category entities.
    /// Use this to avoid EF tracking conflicts when updating product categories.
    /// </summary>
    public void AddCategoriesById(IEnumerable<Guid> categoryIds)
    {
        foreach (var categoryId in categoryIds)
        {
            if (categoryId == Guid.Empty) continue;
            if (_productCategories.Any(pc => pc.CategoryId == categoryId)) continue;

            var shouldBePrimary = !_productCategories.Any();
            if (shouldBePrimary)
            {
                foreach (var pc in _productCategories.Where(pc => pc.IsPrimary))
                {
                    pc.RemovePrimary();
                }
            }

            var link = ProductCategory.CreateById(Id, categoryId, shouldBePrimary);
            // Don't set link.Product - EF will resolve the relationship via ProductId
            _productCategories.Add(link);
        }
    }

    /// <summary>
    /// Sets a category as the primary category for this product.
    /// </summary>
    public void SetPrimaryCategory(Guid categoryId)
    {
        var targetCategory = _productCategories.FirstOrDefault(pc => pc.CategoryId == categoryId);
        if (targetCategory is null) return;

        foreach (var pc in _productCategories)
        {
            if (pc.CategoryId == categoryId)
            {
                pc.SetAsPrimary();
            }
            else if (pc.IsPrimary)
            {
                pc.RemovePrimary();
            }
        }
    }

    /// <summary>
    /// Gets the primary category for breadcrumb navigation.
    /// </summary>
    public ProductCategory? GetPrimaryCategory()
    {
        return _productCategories.FirstOrDefault(pc => pc.IsPrimary)
            ?? _productCategories.FirstOrDefault();
    }

    public void RemoveCategory(Guid categoryId)
    {
        var existing = _productCategories.FirstOrDefault(pc => pc.CategoryId == categoryId);
        if (existing is null)
        {
            return;
        }

        // Note: EF Core handles the relationship automatically
        // No need to call existing.Category?.RemoveProductCategory(existing)
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
        // Note: EF Core handles the relationship automatically via navigation properties
    }

    /// <summary>
    /// Adds tags by their IDs without loading Tag entities.
    /// Use this to avoid EF tracking conflicts when updating product tags.
    /// </summary>
    public void AddTagsById(IEnumerable<Guid> tagIds)
    {
        foreach (var tagId in tagIds)
        {
            if (tagId == Guid.Empty) continue;
            if (_productTags.Any(pt => pt.TagId == tagId)) continue;

            var link = ProductTag.CreateById(Id, tagId);
            // Don't set link.Product - EF will resolve the relationship via ProductId
            _productTags.Add(link);
        }
    }

    public void RemoveTag(Guid tagId)
    {
        var existing = _productTags.FirstOrDefault(pt => pt.TagId == tagId);
        if (existing is null)
        {
            return;
        }

        // Note: EF Core handles the relationship automatically
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