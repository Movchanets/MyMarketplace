using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositories;

public class SearchQueryRepositoryTests : TestBase
{
	private readonly ISearchQueryRepository _searchQueryRepository;

	public SearchQueryRepositoryTests()
	{
		_searchQueryRepository = new SearchQueryRepository(DbContext);
	}

	[Fact]
	public async Task Add_ValidSearchQuery_SavesSuccessfully()
	{
		// Arrange
		var query = SearchQuery.Create("iPhone 15");

		// Act
		_searchQueryRepository.Add(query);
		await DbContext.SaveChangesAsync();

		// Assert
		var saved = await _searchQueryRepository.GetByQueryAsync("iphone 15");
		saved.Should().NotBeNull();
		saved!.Query.Should().Be("iPhone 15");
		saved.NormalizedQuery.Should().Be("iphone 15");
		saved.SearchCount.Should().Be(1);
	}

	[Fact]
	public async Task GetByQueryAsync_WithExistingQuery_ReturnsQuery()
	{
		// Arrange
		var query = SearchQuery.Create("Samsung Galaxy");
		_searchQueryRepository.Add(query);
		await DbContext.SaveChangesAsync();

		// Act
		var result = await _searchQueryRepository.GetByQueryAsync("samsung galaxy");

		// Assert
		result.Should().NotBeNull();
		result!.Query.Should().Be("Samsung Galaxy");
		result.NormalizedQuery.Should().Be("samsung galaxy");
	}

	[Fact]
	public async Task GetByQueryAsync_WithNonExistentQuery_ReturnsNull()
	{
		// Act
		var result = await _searchQueryRepository.GetByQueryAsync("nonexistent query");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByQueryAsync_IsCaseInsensitive()
	{
		// Arrange
		var query = SearchQuery.Create("Laptop ASUS");
		_searchQueryRepository.Add(query);
		await DbContext.SaveChangesAsync();

		// Act - GetByQueryAsync expects normalized (lowercase) input
		var result1 = await _searchQueryRepository.GetByQueryAsync("laptop asus");
		var result2 = await _searchQueryRepository.GetByQueryAsync("laptop asus"); // Same normalized
		var result3 = await _searchQueryRepository.GetByQueryAsync("laptop asus"); // Same normalized

		// Assert
		result1.Should().NotBeNull();
		result2.Should().NotBeNull();
		result3.Should().NotBeNull();
		result1!.Id.Should().Be(result2!.Id).And.Be(result3!.Id);
		
		// Original query should be preserved
		result1.Query.Should().Be("Laptop ASUS");
	}

	[Fact]
	public async Task Update_IncrementCount_UpdatesSuccessfully()
	{
		// Arrange
		var query = SearchQuery.Create("MacBook Pro");
		_searchQueryRepository.Add(query);
		await DbContext.SaveChangesAsync();

		var originalLastSearchedAt = query.LastSearchedAt;
		await Task.Delay(10); // Ensure time difference

		// Act
		var existing = await _searchQueryRepository.GetByQueryAsync("macbook pro");
		existing!.IncrementCount();
		_searchQueryRepository.Update(existing);
		await DbContext.SaveChangesAsync();

		// Assert
		var updated = await _searchQueryRepository.GetByQueryAsync("macbook pro");
		updated.Should().NotBeNull();
		updated!.SearchCount.Should().Be(2);
		updated.LastSearchedAt.Should().BeAfter(originalLastSearchedAt);
	}

	[Fact]
	public async Task GetPopularAsync_ReturnsQueriesOrderedBySearchCount()
	{
		// Arrange
		var query1 = SearchQuery.Create("Query 1");
		var query2 = SearchQuery.Create("Query 2");
		var query3 = SearchQuery.Create("Query 3");

		_searchQueryRepository.Add(query1);
		_searchQueryRepository.Add(query2);
		_searchQueryRepository.Add(query3);
		await DbContext.SaveChangesAsync();

		// Increment counts
		query1.IncrementCount(); // 2
		query1.IncrementCount(); // 3
		query2.IncrementCount(); // 2
		_searchQueryRepository.Update(query1);
		_searchQueryRepository.Update(query2);
		await DbContext.SaveChangesAsync();

		// Act
		var popular = await _searchQueryRepository.GetPopularAsync(10);
		var popularList = popular.ToList();

		// Assert
		popularList.Should().HaveCount(3);
		popularList[0].Query.Should().Be("Query 1");
		popularList[0].SearchCount.Should().Be(3);
		popularList[1].Query.Should().Be("Query 2");
		popularList[1].SearchCount.Should().Be(2);
		popularList[2].Query.Should().Be("Query 3");
		popularList[2].SearchCount.Should().Be(1);
	}

	[Fact]
	public async Task GetPopularAsync_WithLimit_ReturnsLimitedResults()
	{
		// Arrange
		for (int i = 1; i <= 15; i++)
		{
			var query = SearchQuery.Create($"Query {i}");
			for (int j = 0; j < i; j++)
			{
				query.IncrementCount();
			}
			_searchQueryRepository.Add(query);
		}
		await DbContext.SaveChangesAsync();

		// Act
		var popular = await _searchQueryRepository.GetPopularAsync(5);
		var popularList = popular.ToList();

		// Assert
		popularList.Should().HaveCount(5);
		popularList[0].Query.Should().Be("Query 15"); // Highest count
		popularList[4].Query.Should().Be("Query 11"); // 5th highest
	}

	[Fact]
	public async Task GetPopularAsync_WithNoQueries_ReturnsEmptyList()
	{
		// Act
		var popular = await _searchQueryRepository.GetPopularAsync(10);

		// Assert
		popular.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByQueryAsync_WithEmptyString_ReturnsNull()
	{
		// Act
		var result = await _searchQueryRepository.GetByQueryAsync("");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByQueryAsync_WithWhitespace_ReturnsNull()
	{
		// Act
		var result = await _searchQueryRepository.GetByQueryAsync("   ");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task MultipleIncrements_TrackCorrectly()
	{
		// Arrange
		var query = SearchQuery.Create("Popular Product");
		_searchQueryRepository.Add(query);
		await DbContext.SaveChangesAsync();

		// Act - Simulate 10 searches
		for (int i = 0; i < 10; i++)
		{
			var existing = await _searchQueryRepository.GetByQueryAsync("popular product");
			existing!.IncrementCount();
			_searchQueryRepository.Update(existing);
			await DbContext.SaveChangesAsync();
		}

		// Assert
		var final = await _searchQueryRepository.GetByQueryAsync("popular product");
		final.Should().NotBeNull();
		final!.SearchCount.Should().Be(11); // 1 initial + 10 increments
	}
}
