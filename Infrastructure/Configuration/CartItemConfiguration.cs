using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure.Configuration;

/// <summary>
/// EF Core configuration for CartItem entity
/// </summary>
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
	public void Configure(EntityTypeBuilder<CartItem> builder)
	{
		builder.ToTable("CartItems");

		builder.HasKey(i => i.Id);

		builder.Property(i => i.Id)
			.ValueGeneratedOnAdd()
			.HasValueGenerator<SequentialGuidValueGenerator>();

		builder.Property(i => i.Quantity)
			.IsRequired();

		builder.Property(i => i.AddedAt)
			.IsRequired();

		// Composite unique index to prevent duplicate SKU entries in same cart
		builder.HasIndex(i => new { i.CartId, i.SkuId })
			.IsUnique();

		// Performance indexes for common queries
		builder.HasIndex(i => i.CartId);
		builder.HasIndex(i => i.SkuId);
		builder.HasIndex(i => i.ProductId);

		// Foreign key to Product
		builder.HasOne(i => i.Product)
			.WithMany()
			.HasForeignKey(i => i.ProductId)
			.OnDelete(DeleteBehavior.Restrict);

		// Foreign key to SKU
		builder.HasOne(i => i.Sku)
			.WithMany()
			.HasForeignKey(i => i.SkuId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
