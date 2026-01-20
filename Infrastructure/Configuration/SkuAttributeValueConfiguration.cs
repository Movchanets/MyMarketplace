using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class SkuAttributeValueConfiguration : IEntityTypeConfiguration<SkuAttributeValue>
{
	public void Configure(EntityTypeBuilder<SkuAttributeValue> builder)
	{
		builder.ToTable("SkuAttributeValues", t => 
		{
			// Ensure only one value type is set (check constraint)
			t.HasCheckConstraint(
				"CK_SkuAttributeValue_OnlyOneValueType",
				@"(CASE WHEN ""ValueString"" IS NOT NULL THEN 1 ELSE 0 END +
				  CASE WHEN ""ValueNumber"" IS NOT NULL THEN 1 ELSE 0 END +
				  CASE WHEN ""ValueBoolean"" IS NOT NULL THEN 1 ELSE 0 END) = 1"
			);
		});

		builder.HasKey(av => av.Id);

		// Indexes for efficient filtering
		builder.HasIndex(av => av.SkuId)
			.HasDatabaseName("IX_SkuAttributeValues_SkuId");

		builder.HasIndex(av => av.AttributeDefinitionId)
			.HasDatabaseName("IX_SkuAttributeValues_AttributeDefinitionId");

		// Composite indexes for fast filtering by attribute + value
		builder.HasIndex(av => new { av.AttributeDefinitionId, av.ValueString })
			.HasDatabaseName("IX_SkuAttributeValues_AttributeId_ValueString");

		builder.HasIndex(av => new { av.AttributeDefinitionId, av.ValueNumber })
			.HasDatabaseName("IX_SkuAttributeValues_AttributeId_ValueNumber");

		builder.HasIndex(av => new { av.AttributeDefinitionId, av.ValueBoolean })
			.HasDatabaseName("IX_SkuAttributeValues_AttributeId_ValueBoolean");

		// Properties
		builder.Property(av => av.ValueString)
			.HasMaxLength(500)
			.IsRequired(false);

		builder.Property(av => av.ValueNumber)
			.HasPrecision(18, 2)
			.IsRequired(false);

		builder.Property(av => av.ValueBoolean)
			.IsRequired(false);

		// Relationships
		builder.HasOne(av => av.Sku)
			.WithMany(s => s.AttributeValues)
			.HasForeignKey(av => av.SkuId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(av => av.AttributeDefinition)
			.WithMany()
			.HasForeignKey(av => av.AttributeDefinitionId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
