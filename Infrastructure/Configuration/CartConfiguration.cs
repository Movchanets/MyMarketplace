using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure.Configuration;

/// <summary>
/// EF Core configuration for Cart entity
/// Note: Optimistic concurrency is not used for Cart - race conditions in shopping cart
/// don't lead to data corruption, only to potentially stale UI state which is acceptable.
/// </summary>
public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
	public void Configure(EntityTypeBuilder<Cart> builder)
	{
		builder.ToTable("Carts");

		builder.HasKey(c => c.Id);

		builder.Property(c => c.Id)
			.ValueGeneratedOnAdd()
			.HasValueGenerator<SequentialGuidValueGenerator>();

		// Ignore the RowVersion property - not using concurrency control for Cart
		builder.Ignore(c => c.RowVersion);

		// Unique constraint on UserId (one cart per user)
		builder.HasIndex(c => c.UserId)
			.IsUnique();

		// Performance index for user lookups
		builder.HasIndex(c => c.CreatedAt);

		// Configure navigation to Items with cascade delete
		builder.HasMany(c => c.Items)
			.WithOne(i => i.Cart)
			.HasForeignKey(i => i.CartId)
			.OnDelete(DeleteBehavior.Cascade);

		// Configure navigation access mode for encapsulation
		builder.Metadata.FindNavigation(nameof(Cart.Items))?
			.SetPropertyAccessMode(PropertyAccessMode.Field);
	}
}
