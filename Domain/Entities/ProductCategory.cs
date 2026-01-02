namespace Domain.Entities;

public class ProductCategory : BaseEntity<Guid>
{
	public Guid ProductId { get; private set; }
	public virtual Product? Product { get; internal set; }
	public Guid CategoryId { get; private set; }
	public virtual Category? Category { get; private set; }

	/// <summary>
	/// Indicates if this is the primary category for the product.
	/// Used for breadcrumb navigation.
	/// </summary>
	public bool IsPrimary { get; private set; }

	private ProductCategory() { }

	private ProductCategory(Guid productId, Guid categoryId, bool isPrimary = false)
	{
		if (productId == Guid.Empty)
		{
			throw new ArgumentException("ProductId cannot be empty", nameof(productId));
		}

		if (categoryId == Guid.Empty)
		{
			throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));
		}

		Id = Guid.NewGuid();
		ProductId = productId;
		CategoryId = categoryId;
		IsPrimary = isPrimary;
	}

	public static ProductCategory Create(Product product, Category category, bool isPrimary = false)
	{
		if (product is null)
		{
			throw new ArgumentNullException(nameof(product));
		}

		if (category is null)
		{
			throw new ArgumentNullException(nameof(category));
		}

		var link = new ProductCategory(product.Id, category.Id, isPrimary);
		// Only attach Product reference - Category reference would cause EF tracking conflicts
		// when category is loaded with AsNoTracking
		link.Product = product;
		return link;
	}

	/// <summary>
	/// Creates a ProductCategory link using only IDs, without loading the Category entity.
	/// Use this to avoid EF tracking conflicts when updating product categories.
	/// </summary>
	public static ProductCategory CreateById(Guid productId, Guid categoryId, bool isPrimary = false)
	{
		return new ProductCategory(productId, categoryId, isPrimary);
	}

	/// <summary>
	/// Sets this category as primary for the product.
	/// </summary>
	public void SetAsPrimary()
	{
		IsPrimary = true;
	}

	/// <summary>
	/// Removes primary status from this category.
	/// </summary>
	public void RemovePrimary()
	{
		IsPrimary = false;
	}

	public void Attach(Product product, Category category)
	{
		Product = product ?? throw new ArgumentNullException(nameof(product));
		ProductId = product.Id;
		Category = category ?? throw new ArgumentNullException(nameof(category));
		CategoryId = category.Id;
	}

}