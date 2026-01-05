using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class SkuGalleryConfiguration : IEntityTypeConfiguration<SkuGallery>
{
	public void Configure(EntityTypeBuilder<SkuGallery> builder)
	{
		builder.ToTable("SkuGalleries");
		builder.HasKey(sg => sg.Id);

		builder.Property(sg => sg.DisplayOrder)
			.IsRequired();

		builder.HasIndex(sg => new { sg.SkuId, sg.DisplayOrder });

		builder.HasOne(sg => sg.Sku)
			.WithMany(s => s.Gallery)
			.HasForeignKey(sg => sg.SkuId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(sg => sg.MediaImage)
			.WithMany()
			.HasForeignKey(sg => sg.MediaImageId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
