namespace Domain.Entities;

/// <summary>
/// Entity для збереження популярних пошукових запитів
/// </summary>
public class SearchQuery : BaseEntity<Guid>
{
	public string Query { get; private set; } = null!;
	public string NormalizedQuery { get; private set; } = null!;
	public long SearchCount { get; private set; }
	public DateTime LastSearchedAt { get; private set; }

	private SearchQuery() { }

	public static SearchQuery Create(string query)
	{
		if (string.IsNullOrWhiteSpace(query))
			throw new ArgumentNullException(nameof(query));

		return new SearchQuery
		{
			Id = Guid.NewGuid(),
			Query = query.Trim(),
			NormalizedQuery = query.Trim().ToLowerInvariant(),
			SearchCount = 1,
			LastSearchedAt = DateTime.UtcNow
		};
	}

	public void IncrementCount()
	{
		SearchCount++;
		LastSearchedAt = DateTime.UtcNow;
		MarkAsUpdated();
	}
}
