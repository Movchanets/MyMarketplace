using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ProductFavoriteConfiguration : IEntityTypeConfiguration<ProductFavorite>
{
    public void Configure(EntityTypeBuilder<ProductFavorite> builder)
    {
        // 1. Composite Primary Key
        builder.HasKey(e => new { e.UserId, e.ProductId });

        // 2. Relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.Favorites)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. Index for querying "Get User's Favorites" (Fast)
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_ProductFavorites_UserId");

        // 4. Index for querying "Most Favorited Products" (Analytics)
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_ProductFavorites_ProductId");

        // 5. Index for ordering by creation date
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_ProductFavorites_CreatedAt");

        // 6. Configure CreatedAt property
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .ValueGeneratedOnAdd();

        builder.ToTable("ProductFavorites");
    }
}