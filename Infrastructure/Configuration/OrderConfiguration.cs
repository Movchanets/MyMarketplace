using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure.Configuration;

/// <summary>
/// EF Core configuration for Order aggregate root with optimistic concurrency
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
	public void Configure(EntityTypeBuilder<Order> builder)
	{
		builder.ToTable("Orders");

		builder.HasKey(o => o.Id);

		builder.Property(o => o.Id)
			.ValueGeneratedOnAdd()
			.HasValueGenerator<SequentialGuidValueGenerator>();

		// PostgreSQL optimistic concurrency using xmin system column
		builder.Property<uint>("xmin")
			.HasColumnType("xid")
			.ValueGeneratedOnAddOrUpdate()
			.IsConcurrencyToken();

		// Ignore the RowVersion property since we use xmin
		builder.Ignore(o => o.RowVersion);

		// Order number must be unique
		builder.Property(o => o.OrderNumber)
			.IsRequired()
			.HasMaxLength(50);

		builder.HasIndex(o => o.OrderNumber)
			.IsUnique();

		// Idempotency key for duplicate order prevention
		builder.Property(o => o.IdempotencyKey)
			.HasMaxLength(100);

		builder.HasIndex(o => o.IdempotencyKey)
			.IsUnique()
			.HasFilter("\"IdempotencyKey\" IS NOT NULL");

		// Decimal precision for monetary values
		builder.Property(o => o.TotalPrice)
			.HasPrecision(18, 2);

		builder.Property(o => o.DiscountAmount)
			.HasPrecision(18, 2);

		builder.Property(o => o.ShippingCost)
			.HasPrecision(18, 2);

		// Status columns
		builder.Property(o => o.Status)
			.IsRequired();

		builder.Property(o => o.PaymentStatus)
			.IsRequired();

		// Delivery and payment methods
		builder.Property(o => o.DeliveryMethod)
			.IsRequired()
			.HasMaxLength(100);

		builder.Property(o => o.PaymentMethod)
			.IsRequired()
			.HasMaxLength(100);

		// Optional fields
		builder.Property(o => o.PromoCode)
			.HasMaxLength(50);

		builder.Property(o => o.CustomerNotes)
			.HasMaxLength(1000);

		builder.Property(o => o.TrackingNumber)
			.HasMaxLength(100);

		builder.Property(o => o.ShippingCarrier)
			.HasMaxLength(100);

		builder.Property(o => o.CancellationReason)
			.HasMaxLength(500);

		// Performance indexes
		builder.HasIndex(o => o.UserId);
		builder.HasIndex(o => o.Status);
		builder.HasIndex(o => o.PaymentStatus);
		builder.HasIndex(o => o.CreatedAt);
		builder.HasIndex(o => new { o.UserId, o.Status });
		builder.HasIndex(o => new { o.UserId, o.CreatedAt });

		// Configure ShippingAddress as owned entity
		builder.OwnsOne(o => o.ShippingAddress, address =>
		{
			address.Property(a => a.FirstName)
				.HasColumnName("ShippingFirstName")
				.HasMaxLength(100)
				.IsRequired();

			address.Property(a => a.LastName)
				.HasColumnName("ShippingLastName")
				.HasMaxLength(100)
				.IsRequired();

			address.Property(a => a.PhoneNumber)
				.HasColumnName("ShippingPhoneNumber")
				.HasMaxLength(20)
				.IsRequired();

			address.Property(a => a.Email)
				.HasColumnName("ShippingEmail")
				.HasMaxLength(100)
				.IsRequired();

			address.Property(a => a.AddressLine1)
				.HasColumnName("ShippingAddressLine1")
				.HasMaxLength(200)
				.IsRequired();

			address.Property(a => a.AddressLine2)
				.HasColumnName("ShippingAddressLine2")
				.HasMaxLength(200);

			address.Property(a => a.City)
				.HasColumnName("ShippingCity")
				.HasMaxLength(100)
				.IsRequired();

			address.Property(a => a.State)
				.HasColumnName("ShippingState")
				.HasMaxLength(100);

			address.Property(a => a.PostalCode)
				.HasColumnName("ShippingPostalCode")
				.HasMaxLength(20)
				.IsRequired();

			address.Property(a => a.Country)
				.HasColumnName("ShippingCountry")
				.HasMaxLength(100)
				.IsRequired();
		});

		// Configure navigation to Items with cascade delete
		builder.HasMany(o => o.Items)
			.WithOne(i => i.Order)
			.HasForeignKey(i => i.OrderId)
			.OnDelete(DeleteBehavior.Cascade);

		// Configure navigation access mode for encapsulation
		builder.Metadata.FindNavigation(nameof(Order.Items))?
			.SetPropertyAccessMode(PropertyAccessMode.Field);
	}
}
