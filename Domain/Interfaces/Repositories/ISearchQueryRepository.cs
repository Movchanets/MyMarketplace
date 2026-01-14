using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository для роботи з пошуковими запитами
/// </summary>
public interface ISearchQueryRepository
{
	/// <summary>
	/// Gets popular search queries ordered by count
	/// </summary>
	Task<IEnumerable<SearchQuery>> GetPopularAsync(int limit = 10);

	/// <summary>
	/// Finds existing query by normalized text
	/// </summary>
	Task<SearchQuery?> GetByQueryAsync(string normalizedQuery);

	/// <summary>
	/// Adds a new search query
	/// </summary>
	void Add(SearchQuery query);

	/// <summary>
	/// Updates an existing search query
	/// </summary>
	void Update(SearchQuery query);
}
