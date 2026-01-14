using Domain.Helpers;

namespace Domain.Entities;

public class Category : BaseEntity<Guid>
{
	private readonly List<Category> _children = new();
	private readonly List<ProductCategory> _productCategories = new();

	public string Name { get; private set; }
	public string Slug { get; private set; }
	public string? Description { get; private set; }
	public string? Emoji { get; private set; }
	public Guid? ParentCategoryId { get; private set; }
	public virtual Category? ParentCategory { get; private set; }
	public virtual IReadOnlyCollection<Category> Children => _children.AsReadOnly();
	public virtual IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

	private Category() { }

	private Category(string name, string? description, Guid? parentCategoryId)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentNullException(nameof(name));
		}

		Id = Guid.NewGuid();
		Name = name.Trim();
		Description = description?.Trim();
		ParentCategoryId = parentCategoryId;
		Slug = SlugHelper.GenerateSlug(name);
	}

	public static Category Create(string name, string? description = null, Guid? parentCategoryId = null)
	{
		return new Category(name, description, parentCategoryId);
	}

	public void Rename(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentNullException(nameof(name));
		}

		Name = name.Trim();
		Slug = SlugHelper.GenerateSlug(name);
		MarkAsUpdated();
	}

	public void UpdateDescription(string? description)
	{
		Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
		MarkAsUpdated();
	}

	public void SetEmoji(string? emoji)
	{
		Emoji = string.IsNullOrWhiteSpace(emoji) ? null : emoji.Trim();
		MarkAsUpdated();
	}

	public void SetParent(Category? parent)
	{
		if (parent != null && parent.Id == Id)
		{
			throw new InvalidOperationException("Category cannot be its own parent.");
		}

		ParentCategory = parent;
		ParentCategoryId = parent?.Id;
		MarkAsUpdated();
	}

	public void AddChild(Category child)
	{
		if (child is null)
		{
			throw new ArgumentNullException(nameof(child));
		}

		child.SetParent(this);
		_children.Add(child);
	}

	public void AddProductCategory(ProductCategory productCategory)
	{
		if (productCategory is null)
		{
			throw new ArgumentNullException(nameof(productCategory));
		}

		if (_productCategories.Contains(productCategory))
		{
			return;
		}

		_productCategories.Add(productCategory);
	}

	internal void RemoveProductCategory(ProductCategory productCategory)
	{
		if (productCategory is null)
		{
			throw new ArgumentNullException(nameof(productCategory));
		}

		_productCategories.Remove(productCategory);
	}

}