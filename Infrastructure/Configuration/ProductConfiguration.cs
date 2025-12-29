using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Store)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.BaseImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Attributes)
            .HasColumnType("jsonb");

        builder.Metadata.FindNavigation(nameof(Product.ProductTags))?
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Product.Skus))?
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Product.Gallery))?
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Product.ProductCategories))?
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.ProductTags)
            .WithOne(pt => pt.Product)
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Skus)
            .WithOne(s => s.Product)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Gallery)
            .WithOne(g => g.Product)
            .HasForeignKey(g => g.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ProductCategories)
            .WithOne(pc => pc.Product)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}