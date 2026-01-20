namespace Domain.Entities;

/// <summary>
/// Represents a single attribute value for a SKU with typed columns for efficient filtering.
/// This replaces JSONB storage for filterable attributes while maintaining flexibility.
/// </summary>
public class SkuAttributeValue
{
	public Guid Id { get; private set; }
	public Guid SkuId { get; private set; }
	public Guid AttributeDefinitionId { get; private set; }

	/// <summary>
	/// String value (for color, brand, etc.)
	/// </summary>
	public string? ValueString { get; private set; }

	/// <summary>
	/// Numeric value (for storage, RAM, screen size, etc.)
	/// </summary>
	public decimal? ValueNumber { get; private set; }

	/// <summary>
	/// Boolean value (for features like waterproof, wireless, etc.)
	/// </summary>
	public bool? ValueBoolean { get; private set; }

	// Navigation properties
	public SkuEntity Sku { get; private set; } = null!;
	public AttributeDefinition AttributeDefinition { get; private set; } = null!;

	// EF Core constructor
	private SkuAttributeValue() { }

	/// <summary>
	/// Factory method to create a SKU attribute value with automatic type detection
	/// </summary>
	public static SkuAttributeValue Create(
		Guid skuId,
		Guid attributeDefinitionId,
		object? value)
	{
		var attr = new SkuAttributeValue
		{
			Id = Guid.NewGuid(),
			SkuId = skuId,
			AttributeDefinitionId = attributeDefinitionId
		};

		attr.SetValue(value);
		return attr;
	}

	/// <summary>
	/// Set the value with automatic type detection
	/// </summary>
	public void SetValue(object? value)
	{
		// Reset all values
		ValueString = null;
		ValueNumber = null;
		ValueBoolean = null;

		if (value == null) return;

		switch (value)
		{
			case string s:
				ValueString = s;
				break;
			case decimal d:
				ValueNumber = d;
				break;
			case double db:
				ValueNumber = (decimal)db;
				break;
			case float f:
				ValueNumber = (decimal)f;
				break;
			case int i:
				ValueNumber = i;
				break;
			case long l:
				ValueNumber = l;
				break;
			case bool b:
				ValueBoolean = b;
				break;
			default:
				// Fallback: try to parse as string
				ValueString = value.ToString();
				break;
		}
	}

	/// <summary>
	/// Get the value as object (returns the non-null typed value)
	/// </summary>
	public object? GetValue()
	{
		if (ValueString != null) return ValueString;
		if (ValueNumber.HasValue) return ValueNumber.Value;
		if (ValueBoolean.HasValue) return ValueBoolean.Value;
		return null;
	}

	/// <summary>
	/// Get the value as a specific type with validation
	/// </summary>
	public T? GetValue<T>()
	{
		var value = GetValue();
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
	/// Update the value (for edit operations)
	/// </summary>
	public void Update(object? value)
	{
		SetValue(value);
	}
}
