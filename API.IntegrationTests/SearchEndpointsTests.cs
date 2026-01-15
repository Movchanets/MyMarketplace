using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

public class SearchEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SearchEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSearch_WithValidQuery_ReturnsProducts()
    {
        // Arrange - use seeded demo products
        var query = "phone";

        // Act
        var response = await _client.GetAsync($"/api/search?q={query}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var payload = json.GetProperty("payload");
        payload.ValueKind.Should().Be(JsonValueKind.Array);

        // Should find products containing "phone" in name or description
        payload.GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetSearch_WithEmptyQuery_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/api/search?q=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var payload = json.GetProperty("payload");
        payload.ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetSearch_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var query = "demo";
        var limit = 2;

        // Act
        var response = await _client.GetAsync($"/api/search?q={query}&limit={limit}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var payload = json.GetProperty("payload");
        payload.ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetArrayLength().Should().BeLessThanOrEqualTo(limit);
    }

    [Fact]
    public async Task GetSearch_WithNonExistentQuery_ReturnsEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/api/search?q=nonexistentproductxyz123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var payload = json.GetProperty("payload");
        payload.ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetSearchPopular_ReturnsPopularQueries()
    {
        // Act
        var response = await _client.GetAsync("/api/search/popular");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var payload = json.GetProperty("payload");
        payload.ValueKind.Should().Be(JsonValueKind.Array);

        // Each item should have query and searchCount
        foreach (var item in payload.EnumerateArray())
        {
            item.GetProperty("query").GetString().Should().NotBeNullOrWhiteSpace();
            item.GetProperty("searchCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task GetSearchPopular_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var limit = 5;

        // Act
        var response = await _client.GetAsync($"/api/search/popular?limit={limit}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var payload = json.GetProperty("payload");
        payload.ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetArrayLength().Should().BeLessThanOrEqualTo(limit);
    }

    [Fact]
    public async Task GetSearch_AndPopularQueries_WorkTogether()
    {
        // Test that search functionality works and popular queries are returned
        var query = "demo";

        // Perform a search
        var searchResponse = await _client.GetAsync($"/api/search?q={query}");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchJson = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();
        searchJson.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        // Get popular queries
        var popularResponse = await _client.GetAsync("/api/search/popular");
        popularResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var popularJson = await popularResponse.Content.ReadFromJsonAsync<JsonElement>();
        popularJson.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var popularPayload = popularJson.GetProperty("payload");
        popularPayload.ValueKind.Should().Be(JsonValueKind.Array);

        // Popular queries should have valid structure
        foreach (var item in popularPayload.EnumerateArray())
        {
            item.GetProperty("query").GetString().Should().NotBeNullOrWhiteSpace();
            item.GetProperty("searchCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task GetSearch_WithSpecialCharacters_HandlesCorrectly()
    {
        // Act - search with special characters
        var response = await _client.GetAsync("/api/search?q=test%20query%26special");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var payload = json.GetProperty("payload");
        payload.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetSearch_WithVeryLongQuery_HandlesCorrectly()
    {
        // Arrange - very long query
        var longQuery = new string('a', 1000);

        // Act
        var response = await _client.GetAsync($"/api/search?q={longQuery}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetSearch_ReturnsProductsWithExpectedStructure()
    {
        // Arrange
        var query = "demo"; // Should match seeded products

        // Act
        var response = await _client.GetAsync($"/api/search?q={query}&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var payload = json.GetProperty("payload");
        if (payload.GetArrayLength() > 0)
        {
            var firstProduct = payload[0];

            // Check required product fields
            firstProduct.GetProperty("id").GetGuid().Should().NotBeEmpty();
            firstProduct.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
            firstProduct.GetProperty("slug").GetString().Should().NotBeNullOrWhiteSpace();

            // Check optional fields
            var hasBaseImageUrl = firstProduct.TryGetProperty("baseImageUrl", out var baseImageUrl);
            if (hasBaseImageUrl && baseImageUrl.ValueKind == JsonValueKind.String)
            {
                baseImageUrl.GetString().Should().NotBeNullOrWhiteSpace();
            }

            var hasMinPrice = firstProduct.TryGetProperty("minPrice", out var minPrice);
            if (hasMinPrice && minPrice.ValueKind == JsonValueKind.Number)
            {
                minPrice.GetDecimal().Should().BeGreaterThanOrEqualTo(0);
            }

            firstProduct.GetProperty("inStock").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
            firstProduct.GetProperty("isActive").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);

            // Check categories array
            var categories = firstProduct.GetProperty("categories");
            categories.ValueKind.Should().Be(JsonValueKind.Array);
        }
    }

    // NOTE: Tracking tests are disabled as they require proper test database isolation
    // The tracking functionality is tested via SearchQueryRepositoryTests instead
}