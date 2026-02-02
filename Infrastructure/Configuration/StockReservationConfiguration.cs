using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure.Configuration;

/// <summary>
/// EF Core configuration for StockReservation entity
/// </summary>
public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
	public void Configure(EntityTypeBuilder<StockReservation> builder)
	{
		builder.ToTable("StockReservations");

		builder.HasKey(r => r.Id);

		builder.Property(r => r.Id)
			.ValueGeneratedOnAdd()
			.HasValueGenerator<SequentialGuidValueGenerator>();

		builder.Property(r => r.Quantity)
			.IsRequired();

		builder.Property(r => r.Status)
			.IsRequired();

		builder.Property(r => r.CreatedAt)
			.IsRequired();

		builder.Property(r => r.ExpiresAt)
			.IsRequired();

		builder.Property(r => r.SessionId)
			.HasMaxLength(100);

		builder.Property(r => r.IpAddress)
			.HasMaxLength(50);

		builder.Property(r => r.UserAgent)
			.HasMaxLength(500);

		builder.Property(r => r.CancellationReason)
			.HasMaxLength(500);

		// Performance indexes for common queries
		builder.HasIndex(r => r.SkuId);
		builder.HasIndex(r => r.CartId);
		builder.HasIndex(r => r.OrderId);
		builder.HasIndex(r => r.Status);
		builder.HasIndex(r => r.ExpiresAt);
		builder.HasIndex(r => r.SessionId);

		// Composite indexes for cleanup jobs
		builder.HasIndex(r => new { r.Status, r.ExpiresAt });

		// Foreign key to SKU with cascade delete
		builder.HasOne(r => r.Sku)
			.WithMany(s => s.Reservations)
			.HasForeignKey(r => r.SkuId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
