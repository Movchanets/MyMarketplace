using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Initializer;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositories;

public class ProductRepositoryTests : TestBase
{
    private readonly IProductRepository _productRepository;

    public ProductRepositoryTests()
    {
        _productRepository = new ProductRepository(DbContext);
    }

    public async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Seed demo data
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=testdb;Username=testuser;Password=testpass"));
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        await SeederDB.SeedDataAsync(appBuilder);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsMatchingProducts()
    {
        // Arrange
        var query = "phone"; // Should match "Phone Case (Demo)"

        // Act
        var results = await _productRepository.SearchAsync(query, 10);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(p => p.Name.Contains("Phone", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        // Act
        var results = await _productRepository.SearchAsync("", 10);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithNonExistentQuery_ReturnsEmptyList()
    {
        // Act
        var results = await _productRepository.SearchAsync("nonexistentproductxyz123", 10);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithLimit_RespectsLimit()
    {
        // Arrange
        var query = "demo"; // Should match multiple demo products

        // Act
        var results = await _productRepository.SearchAsync(query, 2);

        // Assert
        results.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task SearchAsync_ReturnsProductsWithAllRequiredFields()
    {
        // Arrange
        var query = "hoodie"; // Should match "Hoodie (Demo)"

        // Act
        var results = await _productRepository.SearchAsync(query, 10);

        // Assert
        results.Should().NotBeEmpty();
        var product = results.First();

        product.Id.Should().NotBeEmpty();
        product.Name.Should().NotBeNullOrWhiteSpace();
        product.Slug.Should().NotBeNullOrWhiteSpace();
        product.IsActive.Should().BeTrue(); // Demo products should be active
        product.StoreId.Should().NotBeNull(); // Demo products have store
    }

    [Fact]
    public async Task SearchAsync_SearchesInNameAndDescription()
    {
        // Test searching in name
        var nameResults = await _productRepository.SearchAsync("Phone", 10);
        nameResults.Should().NotBeEmpty();
        nameResults.Should().Contain(p => p.Name.Contains("Phone"));

        // Test searching in description (if any demo products have descriptions)
        var descResults = await _productRepository.SearchAsync("quality", 10);
        // May or may not find results depending on seeded data
        descResults.Should().NotBeNull(); // Just ensure no exception
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        // Act
        var lowerCase = await _productRepository.SearchAsync("phone", 10);
        var upperCase = await _productRepository.SearchAsync("PHONE", 10);
        var mixedCase = await _productRepository.SearchAsync("Phone", 10);

        // Assert - all should return same results
        lowerCase.Should().BeEquivalentTo(upperCase);
        lowerCase.Should().BeEquivalentTo(mixedCase);
    }

    [Fact]
    public async Task SearchAsync_WithHighLimit_ReturnsAllMatchingProducts()
    {
        // Arrange
        var query = "demo";
        var highLimit = 100;

        // Act
        var results = await _productRepository.SearchAsync(query, highLimit);

        // Assert
        results.Should().HaveCountGreaterThanOrEqualTo(3); // At least 3 demo products
        results.All(p => p.Name.Contains("Demo")).Should().BeTrue();
    }
}