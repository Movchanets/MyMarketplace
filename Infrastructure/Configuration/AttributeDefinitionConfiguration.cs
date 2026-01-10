using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
	public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
	{
		builder.ToTable("AttributeDefinitions");
		builder.HasKey(a => a.Id);

		builder.Property(a => a.Code)
			.IsRequired()
			.HasMaxLength(50);

		builder.HasIndex(a => a.Code)
			.IsUnique();

		builder.Property(a => a.Name)
			.IsRequired()
			.HasMaxLength(100);

		builder.Property(a => a.DataType)
			.IsRequired()
			.HasMaxLength(20)
			.HasDefaultValue("string");

		builder.Property(a => a.Description)
			.HasMaxLength(500);

		builder.Property(a => a.Unit)
			.HasMaxLength(20);

		builder.Property(a => a.AllowedValues)
			.HasColumnType("jsonb");

		builder.Property(a => a.DisplayOrder)
			.HasDefaultValue(0);

		builder.Property(a => a.IsActive)
			.HasDefaultValue(true);

		builder.Property(a => a.IsRequired)
			.HasDefaultValue(false);

		builder.Property(a => a.IsVariant)
			.HasDefaultValue(false);
	}
}
