using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
	public void Configure(EntityTypeBuilder<Category> builder)
	{
		builder.ToTable("Categories");
		builder.HasKey(c => c.Id);

		builder.Metadata.FindNavigation(nameof(Category.Children))?
			.SetPropertyAccessMode(PropertyAccessMode.Field);

		builder.Metadata.FindNavigation(nameof(Category.ProductCategories))?
			.SetPropertyAccessMode(PropertyAccessMode.Field);

		builder.Property(c => c.Name)
			.IsRequired()
			.HasMaxLength(200);

		builder.Property(c => c.Slug)
			.IsRequired()
			.HasMaxLength(200);

		builder.HasIndex(c => c.Slug).IsUnique();
		builder.HasIndex(c => c.ParentCategoryId);

		builder.Property(c => c.Description)
			.HasMaxLength(2000);

		builder.Property(c => c.Emoji)
			.HasMaxLength(10);

		builder.HasOne(c => c.ParentCategory)
			.WithMany(c => c.Children)
			.HasForeignKey(c => c.ParentCategoryId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
