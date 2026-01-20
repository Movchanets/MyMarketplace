using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Helpers;

namespace Domain.Entities;

public class SkuEntity : BaseEntity<Guid>
{
	public Guid ProductId { get; private set; }
	public virtual Product? Product { get; private set; }

	/// <summary>
	/// Human-readable SKU code generated from attributes.
	/// Format: attribute-values or "default" if no attributes.
	/// </summary>
	public string SkuCode { get; private set; } = string.Empty;

	public decimal Price { get; private set; }
	public int StockQuantity { get; private set; }
	
	/// <summary>
	/// JSONB storage for rare/non-filterable attributes (backward compatibility).
	/// Use AttributeValues collection for filterable attributes.
	/// </summary>
	public JsonDocument? Attributes { get; private set; }

	/// <summary>
	/// Typed attribute values for efficient filtering and querying.
	/// Replaces JSONB for common filterable attributes like color, storage, RAM, etc.
	/// </summary>
	private readonly List<SkuAttributeValue> _attributeValues = new();
	public virtual IReadOnlyCollection<SkuAttributeValue> AttributeValues => _attributeValues.AsReadOnly();

	/// <summary>
	/// Gallery images specific to this SKU variant.
	/// Used for visual attributes like color or material.
	/// </summary>
	private readonly List<SkuGallery> _gallery = new();
	public virtual IReadOnlyCollection<SkuGallery> Gallery => _gallery.AsReadOnly();

	private SkuEntity() { }

	private SkuEntity(Guid productId, decimal price, int stockQuantity, JsonDocument? attributes)
	{
		if (productId == Guid.Empty)
		{
			throw new ArgumentException("ProductId cannot be empty", nameof(productId));
		}

		if (price < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative");
		}

		if (stockQuantity < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative");
		}

		Id = Guid.NewGuid();
		ProductId = productId;
		Price = price;
		StockQuantity = stockQuantity;
		Attributes = attributes;
		SkuCode = GenerateSkuCode(attributes);
	}

	public static SkuEntity Create(Guid productId, decimal price, int stockQuantity, IDictionary<string, object?>? attributes = null)
	{
		var document = attributes is not null ? SerializeAttributes(attributes) : null;
		return new SkuEntity(productId, price, stockQuantity, document);
	}

	public void AttachProduct(Product product)
	{
		Product = product ?? throw new ArgumentNullException(nameof(product));
		ProductId = product.Id;
		// Regenerate SKU code with product attributes
		RegenerateSkuCode();
		MarkAsUpdated();
	}

	/// <summary>
	/// Regenerates the SKU code using merged product + SKU attributes.
	/// Call this when product attributes change.
	/// </summary>
	public void RegenerateSkuCode()
	{
		SkuCode = GenerateSkuCode(Attributes, Product?.Attributes);
	}

	public void UpdatePrice(decimal price)
	{
		if (price < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative");
		}

		Price = price;
		MarkAsUpdated();
	}

	public void UpdateStock(int stockQuantity)
	{
		if (stockQuantity < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative");
		}

		StockQuantity = stockQuantity;
		MarkAsUpdated();
	}

	public void SetAttribute<T>(string key, T value)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentNullException(nameof(key));
		}

		var attributes = ToMutableAttributes();
		attributes[key] = value;
		ReplaceAttributes(attributes);
	}

	public T? GetAttribute<T>(string key)
	{
		if (string.IsNullOrWhiteSpace(key) || Attributes is null)
		{
			return default;
		}

		if (!Attributes.RootElement.TryGetProperty(key, out var element))
		{
			return default;
		}

		return element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null
			? default
			: element.Deserialize<T>();
	}

	public static string GenerateSkuCode(JsonDocument? attributes)
	{
		return GenerateSkuCode(attributes, null);
	}

	/// <summary>
	/// Generates a human-readable SKU code from merged attributes (product + sku).
	/// Format: value1-value2-hash (e.g., "red-xl-a1b2c3" or "default-a1b2c3").
	/// </summary>
	public static string GenerateSkuCode(JsonDocument? skuAttributes, JsonDocument? productAttributes)
	{
		// Merge product attributes with SKU attributes (SKU overrides product)
		var merged = MergeAttributes(productAttributes, skuAttributes);

		if (merged.Count == 0)
		{
			return "default";
		}

		// Build readable slug from attribute values
		var slugParts = new List<string>();
		foreach (var kvp in merged.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
		{
			var valueStr = kvp.Value?.ToString() ?? "";
			if (!string.IsNullOrWhiteSpace(valueStr))
			{
				// Slugify each value
				try
				{
					var slugValue = SlugHelper.GenerateSlug(valueStr);
					if (!string.IsNullOrEmpty(slugValue) && slugValue != "n-a")
					{
						slugParts.Add(slugValue);
					}
				}
				catch
				{
					// Skip invalid values
				}
			}
		}

		if (slugParts.Count == 0)
		{
			return "default";
		}

		// Combine values into slug
		var baseSlug = string.Join("-", slugParts);
		
		// Truncate if too long (keep room for hash suffix: -XXXXXX = 7 chars)
		const int maxBaseLength = 25; // Leave 7 chars for hash
		if (baseSlug.Length > maxBaseLength)
		{
			baseSlug = baseSlug.Substring(0, maxBaseLength);
		}

		// Add short hash suffix for uniqueness (from canonical string)
		var canonical = BuildCanonicalAttributesString(skuAttributes);
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
		var shortHash = Convert.ToHexString(hash.AsSpan(0, 3)).ToLowerInvariant();

		return $"{baseSlug}-{shortHash}";
	}

	/// <summary>
	/// Merges product-level attributes with SKU-level attributes.
	/// SKU attributes take precedence over product attributes.
	/// </summary>
	private static Dictionary<string, object?> MergeAttributes(JsonDocument? productAttributes, JsonDocument? skuAttributes)
	{
		var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

		// First add product attributes
		if (productAttributes?.RootElement.ValueKind == JsonValueKind.Object)
		{
			foreach (var prop in productAttributes.RootElement.EnumerateObject())
			{
				result[prop.Name] = GetJsonValue(prop.Value);
			}
		}

		// Then override with SKU attributes
		if (skuAttributes?.RootElement.ValueKind == JsonValueKind.Object)
		{
			foreach (var prop in skuAttributes.RootElement.EnumerateObject())
			{
				result[prop.Name] = GetJsonValue(prop.Value);
			}
		}

		return result;
	}

	private static object? GetJsonValue(JsonElement element)
	{
		return element.ValueKind switch
		{
			JsonValueKind.String => element.GetString(),
			JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			JsonValueKind.Null => null,
			_ => element.GetRawText()
		};
	}

	private static JsonDocument SerializeAttributes(IDictionary<string, object?> attributes)
	{
		var payload = JsonSerializer.Serialize(attributes);
		return JsonDocument.Parse(payload);
	}

	private void ReplaceAttributes(IDictionary<string, object?> attributes)
	{
		Attributes?.Dispose();
		Attributes = SerializeAttributes(attributes);
		SkuCode = GenerateSkuCode(Attributes);
		MarkAsUpdated();
	}

	private Dictionary<string, object?> ToMutableAttributes()
	{
		if (Attributes is null)
		{
			return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
		}

		var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(Attributes.RootElement.GetRawText());
		return dictionary ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
	}

	private static string BuildCanonicalAttributesString(JsonDocument? attributes)
	{
		if (attributes is null || attributes.RootElement.ValueKind != JsonValueKind.Object)
		{
			return "default";
		}

		var builder = new StringBuilder();
		foreach (var property in attributes.RootElement.EnumerateObject().OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
		{
			builder.Append(property.Name);
			builder.Append(':');
			builder.Append(property.Value.ToString());
			builder.Append('|');
		}

		return builder.Length == 0 ? "default" : builder.ToString();
	}

	#region Gallery Management

	/// <summary>
	/// Adds an image to the SKU gallery (for visual variants like color).
	/// </summary>
	public void AddGalleryItem(MediaImage mediaImage, int displayOrder = 0)
	{
		if (mediaImage is null)
		{
			throw new ArgumentNullException(nameof(mediaImage));
		}

		if (_gallery.Any(g => g.MediaImageId == mediaImage.Id))
		{
			return; // Image already in gallery
		}

		var galleryItem = SkuGallery.Create(this, mediaImage, displayOrder);
		_gallery.Add(galleryItem);
		MarkAsUpdated();
	}

	/// <summary>
	/// Removes an image from the SKU gallery.
	/// </summary>
	public SkuGallery? RemoveGalleryItem(Guid galleryItemId)
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
		MarkAsUpdated();
		return existing;
	}

	/// <summary>
	/// Removes an image from the SKU gallery by MediaImageId.
	/// </summary>
	public SkuGallery? RemoveGalleryItemByMediaId(Guid mediaImageId)
	{
		if (mediaImageId == Guid.Empty)
		{
			return null;
		}

		var existing = _gallery.FirstOrDefault(g => g.MediaImageId == mediaImageId);
		if (existing is null)
		{
			return null;
		}

		_gallery.Remove(existing);
		MarkAsUpdated();
		return existing;
	}

	#endregion

	#region Attribute Management (Typed + JSONB Hybrid)

	/// <summary>
	/// Set or update a typed attribute value (automatically determines type).
	/// Prefers typed storage (AttributeValues) over JSONB.
	/// </summary>
	/// <param name="attributeDefinitionId">ID of the attribute definition</param>
	/// <param name="value">The value to set (string, number, or boolean)</param>
	public void SetTypedAttribute(Guid attributeDefinitionId, object? value)
	{
		var existing = _attributeValues.FirstOrDefault(av => av.AttributeDefinitionId == attributeDefinitionId);
		
		if (existing != null)
		{
			existing.Update(value);
		}
		else
		{
			var newAttr = SkuAttributeValue.Create(Id, attributeDefinitionId, value);
			_attributeValues.Add(newAttr);
		}
		
		MarkAsUpdated();
	}

	/// <summary>
	/// Set multiple typed attributes at once from a dictionary.
	/// Used during SKU creation/update with AttributeDefinitions.
	/// </summary>
	/// <param name="attributes">Dictionary of attribute code -> value</param>
	/// <param name="attributeDefinitions">Available attribute definitions</param>
	public void SetTypedAttributes(
		IDictionary<string, object?> attributes,
		IEnumerable<AttributeDefinition> attributeDefinitions)
	{
		var attrDefMap = attributeDefinitions.ToDictionary(ad => ad.Code, StringComparer.OrdinalIgnoreCase);

		foreach (var (code, value) in attributes)
		{
			if (attrDefMap.TryGetValue(code, out var attrDef))
			{
				SetTypedAttribute(attrDef.Id, value);
			}
		}
	}

	/// <summary>
	/// Get a typed attribute value by attribute definition ID.
	/// </summary>
	public object? GetTypedAttribute(Guid attributeDefinitionId)
	{
		var attr = _attributeValues.FirstOrDefault(av => av.AttributeDefinitionId == attributeDefinitionId);
		return attr?.GetValue();
	}

	/// <summary>
	/// Get a typed attribute value by code (requires AttributeDefinition to be loaded).
	/// </summary>
	public object? GetTypedAttributeByCode(string code)
	{
		var attr = _attributeValues.FirstOrDefault(av => 
			av.AttributeDefinition.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
		return attr?.GetValue();
	}

	/// <summary>
	/// Get strongly typed attribute value.
	/// Example: sku.GetTypedAttribute<string>("color") or sku.GetTypedAttribute<int>("storage")
	/// </summary>
	public T? GetTypedAttribute<T>(string code)
	{
		var value = GetTypedAttributeByCode(code);
		if (value == null) return default;

		try
		{
			return (T)Convert.ChangeType(value, typeof(T));
		}
		catch
		{
			return default;
		}
	}

	/// <summary>
	/// Remove a typed attribute.
	/// </summary>
	public void RemoveTypedAttribute(Guid attributeDefinitionId)
	{
		var attr = _attributeValues.FirstOrDefault(av => av.AttributeDefinitionId == attributeDefinitionId);
		if (attr != null)
		{
			_attributeValues.Remove(attr);
			MarkAsUpdated();
		}
	}

	/// <summary>
	/// Get all attributes as a unified dictionary (combines typed + JSONB).
	/// Typed attributes take precedence over JSONB.
	/// Useful for display and backward compatibility.
	/// </summary>
	public Dictionary<string, object?> GetAllAttributes()
	{
		var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

		// Add JSONB attributes first
		if (Attributes != null)
		{
			foreach (var prop in Attributes.RootElement.EnumerateObject())
			{
				result[prop.Name] = GetJsonValue(prop.Value);
			}
		}

		// Override with typed attributes (they take precedence)
		foreach (var av in _attributeValues)
		{
			result[av.AttributeDefinition.Code] = av.GetValue();
		}

		return result;
	}

	/// <summary>
	/// Clear all typed attributes (useful for bulk updates).
	/// </summary>
	public void ClearTypedAttributes()
	{
		_attributeValues.Clear();
		MarkAsUpdated();
	}

	#endregion
}
