using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository для роботи з пошуковими запитами
/// </summary>
public class SearchQueryRepository : ISearchQueryRepository
{
	private readonly AppDbContext _db;

	public SearchQueryRepository(AppDbContext db)
	{
		_db = db;
	}

	public async Task<IEnumerable<SearchQuery>> GetPopularAsync(int limit = 10)
	{
		return await _db.SearchQueries
			.OrderByDescending(s => s.SearchCount)
			.ThenByDescending(s => s.LastSearchedAt)
			.Take(limit)
			.ToListAsync();
	}

	public async Task<SearchQuery?> GetByQueryAsync(string normalizedQuery)
	{
		if (string.IsNullOrWhiteSpace(normalizedQuery))
		{
			return null;
		}

		return await _db.SearchQueries
			.FirstOrDefaultAsync(s => s.NormalizedQuery == normalizedQuery);
	}

	public void Add(SearchQuery query)
	{
		_db.SearchQueries.Add(query);
	}

	public void Update(SearchQuery query)
	{
		_db.SearchQueries.Update(query);
	}
}
