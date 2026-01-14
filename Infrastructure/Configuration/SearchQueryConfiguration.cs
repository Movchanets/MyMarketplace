using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class SearchQueryConfiguration : IEntityTypeConfiguration<SearchQuery>
{
	public void Configure(EntityTypeBuilder<SearchQuery> builder)
	{
		builder.ToTable("SearchQueries");
		builder.HasKey(s => s.Id);

		builder.Property(s => s.Query)
			.IsRequired()
			.HasMaxLength(500);

		builder.Property(s => s.NormalizedQuery)
			.IsRequired()
			.HasMaxLength(500);

		builder.HasIndex(s => s.NormalizedQuery)
			.IsUnique();

		builder.HasIndex(s => s.SearchCount);
		builder.HasIndex(s => s.LastSearchedAt);
	}
}
