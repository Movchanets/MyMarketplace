using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
	public void Configure(EntityTypeBuilder<ProductCategory> builder)
	{
		builder.ToTable("ProductCategories");
		builder.HasKey(pc => pc.Id);

		builder.HasIndex(pc => new { pc.ProductId, pc.CategoryId }).IsUnique();

		// Index for quickly finding primary category
		builder.HasIndex(pc => new { pc.ProductId, pc.IsPrimary })
			.HasFilter("\"IsPrimary\" = true");

		builder.Property(pc => pc.IsPrimary)
			.HasDefaultValue(false);

		builder.HasOne(pc => pc.Product)
			.WithMany(p => p.ProductCategories)
			.HasForeignKey(pc => pc.ProductId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(pc => pc.Category)
			.WithMany(c => c.ProductCategories)
			.HasForeignKey(pc => pc.CategoryId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
