using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Initializer;
using Infrastructure.Repositories;
using Infrastructure.Entities.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositories;

public class ProductRepositoryTests : TestBase
{
    private readonly IProductRepository _productRepository;

    public ProductRepositoryTests()
    {
        _productRepository = new ProductRepository(DbContext);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsMatchingProducts()
    {
        // Arrange
        await CreateSearchTestDataAsync();
        var query = "phone"; // Should match "Phone Case (Demo)"

        // Act
        var results = await _productRepository.SearchAsync(query, 10);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(p => p.Name.Contains("Phone", StringComparison.OrdinalIgnoreCase));
    }

    // Helper: Creates standard test data with 3 products
    private async Task CreateSearchTestDataAsync()
    {
        var store = await CreateVerifiedStoreAsync();
        await CreateProductAsync("Phone Case (Demo)", "High quality phone protection", store.Id, 99.99m, 10);
        await CreateProductAsync("Hoodie (Demo)", "Comfortable hoodie for everyday wear", store.Id, 49.99m, 5);
        await CreateProductAsync("Watch (Demo)", "Stylish watch with great features", store.Id, 199.99m, 3);
    }

    // Helper: Creates a verified store with owner
    private async Task<Store> CreateVerifiedStoreAsync()
    {
        // Create identity user
        var identityUser = new ApplicationUser
        {
            Email = $"owner_{Guid.NewGuid()}@test.com",
            UserName = $"owner_{Guid.NewGuid()}",
            EmailConfirmed = true
        };
        await UserManager.CreateAsync(identityUser, "Test123!");
        
        // Create domain user
        var domainUser = new User(identityUser.Id, "Store", "Owner", identityUser.Email);
        UserRepository.Add(domainUser);
        await DbContext.SaveChangesAsync();
        
        // Create verified store
        var store = Store.Create(domainUser.Id, "Test Store", "Test store description");
        store.Verify(); // CRITICAL: Must be verified for SearchAsync
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();
        
        return store;
    }

    // Helper: Creates an active product with SKU
    private async Task<Product> CreateProductAsync(string name, string description, Guid storeId, decimal price, int stock)
    {
        var product = new Product(name, description);
        product.Activate();
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();
        
        // Set StoreId (private setter workaround)
        DbContext.Entry(product).Property("StoreId").CurrentValue = storeId;
        
        // Create SKU
        var sku = SkuEntity.Create(product.Id, price, stock);
        DbContext.Skus.Add(sku);
        await DbContext.SaveChangesAsync();
        
        return product;
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
        await CreateSearchTestDataAsync();
        var query = "hoodie"; // Should match "Hoodie (Demo)"

        // Act
        var results = await _productRepository.SearchAsync(query, 10);

        // Assert
        results.Should().NotBeEmpty();
        var product = results.First();

        product.Id.Should().NotBeEmpty();
        product.Name.Should().NotBeNullOrWhiteSpace();
        product.Slug.Should().NotBeNullOrWhiteSpace();
        product.IsActive.Should().BeTrue();
        product.StoreId.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchAsync_SearchesInNameAndDescription()
    {
        // Arrange
        await CreateSearchTestDataAsync();
        
        // Act - Test searching in name
        var nameResults = await _productRepository.SearchAsync("Phone", 10);
        
        // Assert
        nameResults.Should().NotBeEmpty();
        nameResults.Should().Contain(p => p.Name.Contains("Phone", StringComparison.OrdinalIgnoreCase));

        // Act - Test searching in description
        var descResults = await _productRepository.SearchAsync("quality", 10);
        
        // Assert
        descResults.Should().NotBeEmpty();
        descResults.Should().Contain(p => p.Name.Contains("Phone", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        // Arrange
        await CreateSearchTestDataAsync();
        
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
        await CreateSearchTestDataAsync();
        var query = "demo";
        var highLimit = 100;

        // Act
        var results = await _productRepository.SearchAsync(query, highLimit);

        // Assert
        results.Should().HaveCount(3); // Exactly 3 demo products
        results.All(p => p.Name.Contains("Demo", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }
}