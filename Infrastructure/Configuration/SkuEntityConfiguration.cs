using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class SkuEntityConfiguration : IEntityTypeConfiguration<SkuEntity>
{
	public void Configure(EntityTypeBuilder<SkuEntity> builder)
	{
		builder.ToTable("Skus");
		builder.HasKey(s => s.Id);

		builder.Property(s => s.SkuCode)
			.IsRequired()
			.HasMaxLength(32);

		builder.Property(s => s.Price)
			.HasPrecision(18, 2)
			.IsRequired();

		builder.Property(s => s.StockQuantity)
			.IsRequired();

		builder.Property(s => s.Attributes)
			.HasColumnType("jsonb");

		builder.HasIndex(s => new { s.ProductId, s.SkuCode }).IsUnique();

		// Index for direct SkuCode lookups (GET /products/s/{productSlug}?sku={skuCode})
		builder.HasIndex(s => s.SkuCode);

		builder.HasOne<Product>(s => s.Product)
			.WithMany(p => p.Skus)
			.HasForeignKey(s => s.ProductId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
