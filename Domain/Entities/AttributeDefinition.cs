using System.Text.Json;

namespace Domain.Entities;

/// <summary>
/// Defines an attribute that can be used for Products and SKUs.
/// Acts as a registry/dictionary of allowed attributes in the system.
/// </summary>
public class AttributeDefinition : BaseEntity<Guid>
{
	/// <summary>
	/// Unique code used as the key in JSONB attributes (e.g., "color", "storage", "brand").
	/// Must be lowercase, alphanumeric with underscores only.
	/// </summary>
	public string Code { get; private set; } = string.Empty;

	/// <summary>
	/// Human-readable name for display (e.g., "Color", "Storage Capacity", "Brand").
	/// </summary>
	public string Name { get; private set; } = string.Empty;

	/// <summary>
	/// Data type for validation: "string", "number", "boolean", "array".
	/// </summary>
	public string DataType { get; private set; } = "string";

	/// <summary>
	/// Whether this attribute is required for all products/SKUs.
	/// </summary>
	public bool IsRequired { get; private set; }

	/// <summary>
	/// Whether this attribute can vary between SKUs (e.g., color, size).
	/// If false, it can only be set at Product level.
	/// </summary>
	public bool IsVariant { get; private set; }

	/// <summary>
	/// Optional predefined values for selection (stored as JSON array).
	/// E.g., ["Red", "Blue", "Green"] for color attribute.
	/// </summary>
	public JsonDocument? AllowedValues { get; private set; }

	/// <summary>
	/// Display order for UI sorting.
	/// </summary>
	public int DisplayOrder { get; private set; }

	/// <summary>
	/// Optional description/hint for the attribute.
	/// </summary>
	public string? Description { get; private set; }

	/// <summary>
	/// Unit of measurement if applicable (e.g., "GB", "mm", "kg").
	/// </summary>
	public string? Unit { get; private set; }

	/// <summary>
	/// Whether this attribute is active and can be assigned to new products.
	/// </summary>
	public bool IsActive { get; private set; } = true;

	private AttributeDefinition() { }

	public AttributeDefinition(
		string code,
		string name,
		string dataType = "string",
		bool isRequired = false,
		bool isVariant = false,
		string? description = null,
		string? unit = null,
		int displayOrder = 0)
	{
		ValidateCode(code);
		ValidateDataType(dataType);

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Name is required", nameof(name));
		}

		Id = Guid.NewGuid();
		Code = NormalizeCode(code);
		Name = name.Trim();
		DataType = dataType.ToLowerInvariant();
		IsRequired = isRequired;
		IsVariant = isVariant;
		Description = description?.Trim();
		Unit = unit?.Trim();
		DisplayOrder = displayOrder;
		IsActive = true;
	}

	public void Update(
		string name,
		string dataType,
		bool isRequired,
		bool isVariant,
		string? description,
		string? unit,
		int displayOrder)
	{
		ValidateDataType(dataType);

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Name is required", nameof(name));
		}

		Name = name.Trim();
		DataType = dataType.ToLowerInvariant();
		IsRequired = isRequired;
		IsVariant = isVariant;
		Description = description?.Trim();
		Unit = unit?.Trim();
		DisplayOrder = displayOrder;
		MarkAsUpdated();
	}

	public void SetAllowedValues(IEnumerable<string>? values)
	{
		if (values is null || !values.Any())
		{
			AllowedValues?.Dispose();
			AllowedValues = null;
		}
		else
		{
			AllowedValues?.Dispose();
			var json = JsonSerializer.Serialize(values.ToArray());
			AllowedValues = JsonDocument.Parse(json);
		}
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

	private static string NormalizeCode(string code)
	{
		return code.Trim().ToLowerInvariant().Replace(" ", "_");
	}

	private static void ValidateCode(string code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			throw new ArgumentException("Code is required", nameof(code));
		}

		var normalized = NormalizeCode(code);
		if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[a-z][a-z0-9_]*$"))
		{
			throw new ArgumentException(
				"Code must start with a letter and contain only lowercase letters, numbers, and underscores",
				nameof(code));
		}

		if (normalized.Length > 50)
		{
			throw new ArgumentException("Code must be 50 characters or less", nameof(code));
		}
	}

	private static void ValidateDataType(string dataType)
	{
		var validTypes = new[] { "string", "number", "boolean", "array" };
		if (!validTypes.Contains(dataType.ToLowerInvariant()))
		{
			throw new ArgumentException(
				$"Invalid data type. Must be one of: {string.Join(", ", validTypes)}",
				nameof(dataType));
		}
	}

	public IReadOnlyList<string>? GetAllowedValuesList()
	{
		if (AllowedValues is null) return null;

		try
		{
			return AllowedValues.RootElement
				.EnumerateArray()
				.Select(e => e.GetString() ?? string.Empty)
				.ToList()
				.AsReadOnly();
		}
		catch
		{
			return null;
		}
	}
}
