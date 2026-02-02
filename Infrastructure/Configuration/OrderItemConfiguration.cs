using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure.Configuration;

/// <summary>
/// EF Core configuration for OrderItem entity with historical snapshots
/// </summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
	public void Configure(EntityTypeBuilder<OrderItem> builder)
	{
		builder.ToTable("OrderItems");

		builder.HasKey(i => i.Id);

		builder.Property(i => i.Id)
			.ValueGeneratedOnAdd()
			.HasValueGenerator<SequentialGuidValueGenerator>();

		builder.Property(i => i.Quantity)
			.IsRequired();

		// Decimal precision for price
		builder.Property(i => i.PriceAtPurchase)
			.HasPrecision(18, 2)
			.IsRequired();

		// Historical snapshot fields (preserved even if original product is deleted)
		builder.Property(i => i.ProductNameSnapshot)
			.IsRequired()
			.HasMaxLength(500);

		builder.Property(i => i.ProductImageUrlSnapshot)
			.HasMaxLength(1000);

		builder.Property(i => i.SkuCodeSnapshot)
			.IsRequired()
			.HasMaxLength(100);

		builder.Property(i => i.SkuAttributesSnapshot)
			.HasColumnType("text");

		// Performance indexes
		builder.HasIndex(i => i.OrderId);
		builder.HasIndex(i => i.ProductId);
		builder.HasIndex(i => i.SkuId);

		// Foreign key to Product (nullable - product may be deleted)
		builder.HasOne(i => i.Product)
			.WithMany()
			.HasForeignKey(i => i.ProductId)
			.OnDelete(DeleteBehavior.SetNull);

		// Foreign key to SKU (nullable - SKU may be deleted)
		builder.HasOne(i => i.Sku)
			.WithMany()
			.HasForeignKey(i => i.SkuId)
			.OnDelete(DeleteBehavior.SetNull);
	}
}
