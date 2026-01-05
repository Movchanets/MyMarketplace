using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Initializer;
using Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests;

public class CatalogCreateEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
	private readonly TestWebApplicationFactory _factory;
	private readonly HttpClient _client;

	public CatalogCreateEndpointsTests(TestWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();
	}

	private async Task<string> LoginAndGetAccessToken(string email, string password)
	{
		var resp = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
		resp.EnsureSuccessStatusCode();
		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		return json.GetProperty("accessToken").GetString()!;
	}

	private async Task<string> LoginAdminAndGetAccessToken()
		=> await LoginAndGetAccessToken("admin@example.com", "Qwerty-1!");

	private async Task<(string Email, string Password)> RegisterUser()
	{
		var email = $"seller_{Guid.NewGuid():N}@example.com";
		var password = "User123!";
		var reg = new { email, name = "Test", surname = "Seller", password, confirmPassword = password };
		var resp = await _client.PostAsJsonAsync("/api/auth/register", reg);
		resp.EnsureSuccessStatusCode();
		return (email, password);
	}

	private async Task EnsureUserInRole(string email, string roleName)
	{
		using var scope = _factory.Services.CreateScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<RoleEntity>>();

		var roleExists = await roleManager.RoleExistsAsync(roleName);
		roleExists.Should().BeTrue($"Seeder should create role '{roleName}'");

		var user = await userManager.FindByEmailAsync(email);
		user.Should().NotBeNull();
		var add = await userManager.AddToRoleAsync(user!, roleName);
		add.Succeeded.Should().BeTrue(string.Join("; ", add.Errors.Select(e => e.Description)));
	}

	private async Task<Guid> GetMyDomainUserId(string accessToken)
	{
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		var resp = await _client.GetAsync("/api/users/me");
		resp.EnsureSuccessStatusCode();
		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		return json.GetProperty("payload").GetProperty("id").GetGuid();
	}

	private async Task<Guid> GetDomainUserIdByEmail(string email)
	{
		using var scope = _factory.Services.CreateScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var appUser = await userManager.FindByEmailAsync(email);
		appUser.Should().NotBeNull($"Expected identity user '{email}' to exist");

		if (appUser!.DomainUserId is Guid domainUserId)
		{
			var exists = await db.DomainUsers.AnyAsync(u => u.Id == domainUserId);
			exists.Should().BeTrue("Domain user should exist for the identity user");
			return domainUserId;
		}

		var domainUser = await db.DomainUsers.FirstOrDefaultAsync(u => u.IdentityUserId == appUser.Id);
		domainUser.Should().NotBeNull("Domain user should be created during registration");
		return domainUser!.Id;
	}

	private async Task EnsureStoreForUser(Guid domainUserId)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var hasStore = await db.Stores.AnyAsync(s => s.UserId == domainUserId);
		if (hasStore)
		{
			return;
		}

		var store = Store.Create(domainUserId, $"Store {Guid.NewGuid():N}");
		db.Stores.Add(store);
		await db.SaveChangesAsync();
	}

	private async Task<Guid> CreateCategoryAsAdmin(string adminToken)
	{
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
		var resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Cat " + Guid.NewGuid().ToString("N"), description = "d" });
		resp.StatusCode.Should().Be(HttpStatusCode.OK);
		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		return json.GetProperty("payload").GetGuid();
	}

	private async Task<Guid> CreateTagAsAdmin(string adminToken)
	{
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
		var resp = await _client.PostAsJsonAsync("/api/tags", new { name = "Tag " + Guid.NewGuid().ToString("N"), description = "d" });
		resp.StatusCode.Should().Be(HttpStatusCode.OK);
		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		return json.GetProperty("payload").GetGuid();
	}

	private async Task<(Guid ProductId, string SkuCode, Guid CategoryId)> GetSeededDemoProductAndCategory()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var category = await db.Categories.FirstAsync(c => c.Slug == "electronics");
		var product = await db.Products
			.Include(p => p.Skus)
			.Include(p => p.ProductCategories)
			.FirstAsync(p => p.Name == "Phone Case (Demo)");

		product.Skus.Should().NotBeEmpty("Seeder should create at least one SKU for demo products");
		var skuCode = product.Skus.First().SkuCode;
		skuCode.Should().NotBeNullOrWhiteSpace();

		return (product.Id, skuCode, category.Id);
	}

	[Fact]
	public async Task PostCategories_AsAdmin_CreatesCategory()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Test Category", description = "desc" });
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		json.GetProperty("payload").GetGuid().Should().NotBeEmpty();
	}

	[Fact]
	public async Task PostTags_AsAdmin_CreatesTag()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var resp = await _client.PostAsJsonAsync("/api/tags", new { name = "Test Tag", description = "desc" });
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		json.GetProperty("payload").GetGuid().Should().NotBeEmpty();
	}

	[Fact]
	public async Task PostProducts_AsSeller_CreatesProduct()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		// Make the user a seller, then login to get a token with seller permissions.
		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);
		var tagId = await CreateTagAsAdmin(adminToken);

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var req = new
		{
			name = "Test Product",
			description = "desc",
			categoryIds = new[] { categoryId },
			price = 10.5m,
			stockQuantity = 3,
			attributes = new { color = "red" },
			tagIds = new[] { tagId }
		};

		var resp = await _client.PostAsJsonAsync("/api/products", req);
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		json.GetProperty("payload").GetGuid().Should().NotBeEmpty();
	}

	[Fact]
	public async Task Seeder_InTesting_SeedsDemoCatalogData()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var adminUser = await db.DomainUsers.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
		adminUser.Should().NotBeNull("Seeder should create the admin domain user in a fresh Testing database");

		var store = await db.Stores.FirstOrDefaultAsync(s => s.UserId == adminUser!.Id);
		store.Should().NotBeNull("Seeder should create an admin store in Development/Testing");
		store!.IsVerified.Should().BeTrue("Seeder verifies the admin demo store");

		var categorySlugs = await db.Categories
			.Where(c => c.Slug == "electronics" || c.Slug == "accessories" || c.Slug == "clothing")
			.Select(c => c.Slug)
			.ToListAsync();
		categorySlugs.Should().BeEquivalentTo(new[] { "electronics", "accessories", "clothing" });

		var tagSlugs = await db.Tags
			.Where(t => t.Slug == "new" || t.Slug == "popular" || t.Slug == "sale")
			.Select(t => t.Slug)
			.ToListAsync();
		tagSlugs.Should().BeEquivalentTo(new[] { "new", "popular", "sale" });

		var productNames = new[] { "Phone Case (Demo)", "Hoodie (Demo)", "USB-C Cable (Demo)" };
		var demoProducts = await db.Products
			.Where(p => p.StoreId == store.Id && productNames.Contains(p.Name))
			.Include(p => p.Skus)
			.Include(p => p.Gallery)
			.ToListAsync();

		demoProducts.Should().HaveCount(3);
		demoProducts.All(p => !string.IsNullOrWhiteSpace(p.BaseImageUrl)).Should().BeTrue();
		demoProducts.Sum(p => p.Skus.Count).Should().BeGreaterThanOrEqualTo(3);
		demoProducts.Sum(p => p.Gallery.Count).Should().BeGreaterThanOrEqualTo(5);

		var seededMediaCount = await db.MediaImages.CountAsync(m => m.StorageKey.StartsWith("seed/catalog/"));
		seededMediaCount.Should().Be(5);
	}

	[Fact]
	public async Task Seeder_Rerun_IsIdempotent_ForDemoCatalogData()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var adminUser = await db.DomainUsers.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
		adminUser.Should().NotBeNull();
		var store = await db.Stores.FirstOrDefaultAsync(s => s.UserId == adminUser!.Id);
		store.Should().NotBeNull();

		var initialCategoryCount = await db.Categories.CountAsync(c => c.Slug == "electronics" || c.Slug == "accessories" || c.Slug == "clothing");
		var initialTagCount = await db.Tags.CountAsync(t => t.Slug == "new" || t.Slug == "popular" || t.Slug == "sale");
		var initialSeededMediaCount = await db.MediaImages.CountAsync(m => m.StorageKey.StartsWith("seed/catalog/"));
		var initialDemoProductsCount = await db.Products.CountAsync(p => p.StoreId == store!.Id &&
			(p.Name == "Phone Case (Demo)" || p.Name == "Hoodie (Demo)" || p.Name == "USB-C Cable (Demo)"));

		var appBuilder = new ApplicationBuilderWrapper(_factory.Services);
		await SeederDB.SeedDataAsync(appBuilder);

		var afterCategoryCount = await db.Categories.CountAsync(c => c.Slug == "electronics" || c.Slug == "accessories" || c.Slug == "clothing");
		var afterTagCount = await db.Tags.CountAsync(t => t.Slug == "new" || t.Slug == "popular" || t.Slug == "sale");
		var afterSeededMediaCount = await db.MediaImages.CountAsync(m => m.StorageKey.StartsWith("seed/catalog/"));
		var afterDemoProductsCount = await db.Products.CountAsync(p => p.StoreId == store!.Id &&
			(p.Name == "Phone Case (Demo)" || p.Name == "Hoodie (Demo)" || p.Name == "USB-C Cable (Demo)"));

		afterCategoryCount.Should().Be(initialCategoryCount);
		afterTagCount.Should().Be(initialTagCount);
		afterSeededMediaCount.Should().Be(initialSeededMediaCount);
		afterDemoProductsCount.Should().Be(initialDemoProductsCount);
	}

	[Fact]
	public async Task GetCategories_Anonymous_ReturnsSeededCategories()
	{
		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync("/api/categories");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var payload = json.GetProperty("payload");
		payload.ValueKind.Should().Be(JsonValueKind.Array);

		var slugs = payload.EnumerateArray().Select(e => e.GetProperty("slug").GetString()).ToList();
		slugs.Should().Contain("electronics");
	}

	[Fact]
	public async Task GetCategoryBySlug_Anonymous_ReturnsCategory()
	{
		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync("/api/categories/slug/electronics");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		json.GetProperty("payload").GetProperty("slug").GetString().Should().Be("electronics");
	}

	[Fact]
	public async Task GetTags_Anonymous_ReturnsSeededTags()
	{
		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync("/api/tags");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var payload = json.GetProperty("payload");
		payload.ValueKind.Should().Be(JsonValueKind.Array);

		var slugs = payload.EnumerateArray().Select(e => e.GetProperty("slug").GetString()).ToList();
		slugs.Should().Contain("new");
	}

	[Fact]
	public async Task GetTagBySlug_Anonymous_ReturnsTag()
	{
		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync("/api/tags/slug/new");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		json.GetProperty("payload").GetProperty("slug").GetString().Should().Be("new");
	}

	[Fact]
	public async Task GetProducts_Anonymous_ReturnsSeededProducts()
	{
		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync("/api/products");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var payload = json.GetProperty("payload");
		payload.ValueKind.Should().Be(JsonValueKind.Array);

		var names = payload.EnumerateArray().Select(e => e.GetProperty("name").GetString()).ToList();
		names.Should().Contain("Phone Case (Demo)");
	}

	[Fact]
	public async Task GetProductById_Anonymous_ReturnsDetailsWithSkusAndGallery()
	{
		var (productId, _, _) = await GetSeededDemoProductAndCategory();

		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync($"/api/products/{productId}");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var payload = json.GetProperty("payload");
		payload.GetProperty("id").GetGuid().Should().Be(productId);

		payload.GetProperty("skus").ValueKind.Should().Be(JsonValueKind.Array);
		payload.GetProperty("skus").GetArrayLength().Should().BeGreaterThan(0);

		payload.GetProperty("gallery").ValueKind.Should().Be(JsonValueKind.Array);
		payload.GetProperty("gallery").GetArrayLength().Should().BeGreaterThan(0);

		var firstGallery = payload.GetProperty("gallery").EnumerateArray().First();
		firstGallery.GetProperty("storageKey").GetString().Should().NotBeNullOrWhiteSpace();
		firstGallery.GetProperty("url").GetString().Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task GetProductBySkuCode_Anonymous_ReturnsSameProductAsById()
	{
		var (productId, skuCode, _) = await GetSeededDemoProductAndCategory();

		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync($"/api/products/by-sku/{skuCode}");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		json.GetProperty("payload").GetProperty("id").GetGuid().Should().Be(productId);
	}

	[Fact]
	public async Task GetProductsByCategory_Anonymous_ReturnsProducts()
	{
		var (_, _, categoryId) = await GetSeededDemoProductAndCategory();

		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync($"/api/products/by-category/{categoryId}");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		json.GetProperty("payload").ValueKind.Should().Be(JsonValueKind.Array);
		json.GetProperty("payload").GetArrayLength().Should().BeGreaterThan(0);
	}

	#region GetProductBySlug Tests

	private async Task<(string ProductSlug, string SkuCode, Guid CategoryId)> GetSeededDemoProductSlugAndSkuCode()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var category = await db.Categories.FirstAsync(c => c.Slug == "electronics");
		var product = await db.Products
			.Include(p => p.Skus)
			.FirstAsync(p => p.Name == "Phone Case (Demo)");

		product.Skus.Should().NotBeEmpty("Seeder should create at least one SKU");
		var skuCode = product.Skus.First().SkuCode;

		return (product.Slug, skuCode, category.Id);
	}

	[Fact]
	public async Task GetProductBySlug_Anonymous_ReturnsProductDetails()
	{
		var (productSlug, _, _) = await GetSeededDemoProductSlugAndSkuCode();

		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync($"/api/products/s/{productSlug}");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var payload = json.GetProperty("payload");
		payload.GetProperty("slug").GetString().Should().Be(productSlug);
		payload.GetProperty("name").GetString().Should().Be("Phone Case (Demo)");
		payload.GetProperty("skus").GetArrayLength().Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task GetProductBySlug_WithSkuCode_ReordersSkusWithMatchingFirst()
	{
		// Create a product with multiple SKUs
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();
		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);

		var req = new
		{
			name = "Multi-Variant Product",
			categoryIds = new[] { categoryId },
			skus = new[]
			{
				new { price = 100.0m, stockQuantity = 10, attributes = new { size = "S" } },
				new { price = 110.0m, stockQuantity = 5, attributes = new { size = "M" } },
				new { price = 120.0m, stockQuantity = 3, attributes = new { size = "L" } }
			}
		};

		var createResp = await _client.PostAsJsonAsync("/api/products", req);
		createResp.StatusCode.Should().Be(HttpStatusCode.OK);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Get slug and last SKU code from DB
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var product = await db.Products.Include(p => p.Skus).FirstAsync(p => p.Id == productId);
		var lastSku = product.Skus.Last();

		// Fetch by slug with specific sku code
		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync($"/api/products/s/{product.Slug}?sku={lastSku.SkuCode}");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var skus = json.GetProperty("payload").GetProperty("skus");
		skus.GetArrayLength().Should().Be(3);
		skus[0].GetProperty("skuCode").GetString().Should().Be(lastSku.SkuCode);
	}

	[Fact]
	public async Task GetProductBySlug_NonExistentSlug_ReturnsNotFound()
	{
		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync("/api/products/s/non-existent-product-slug-xyz123");
		resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
		json.GetProperty("message").GetString().Should().Contain("not found");
	}

	[Fact]
	public async Task GetProductBySlug_WithInvalidSkuCode_StillReturnsProduct()
	{
		var (productSlug, _, _) = await GetSeededDemoProductSlugAndSkuCode();

		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync($"/api/products/s/{productSlug}?sku=invalid-sku-code");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		json.GetProperty("payload").GetProperty("slug").GetString().Should().Be(productSlug);
	}

	[Fact]
	public async Task GetProductBySlug_ReturnsGalleryAndCategories()
	{
		var (productSlug, _, _) = await GetSeededDemoProductSlugAndSkuCode();

		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync($"/api/products/s/{productSlug}");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var payload = json.GetProperty("payload");

		// Verify gallery is included
		payload.GetProperty("gallery").ValueKind.Should().Be(JsonValueKind.Array);

		// Verify categories are included
		payload.GetProperty("categories").ValueKind.Should().Be(JsonValueKind.Array);
	}

	#endregion

	#region Product with Multiple SKUs Tests

	[Fact]
	public async Task PostProducts_WithMultipleSkus_CreatesProductWithAllSkus()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var req = new
		{
			name = "Multi-SKU Product",
			description = "Product with multiple variants",
			categoryIds = new[] { categoryId },
			skus = new[]
			{
				new { price = 100.0m, stockQuantity = 10, attributes = new { size = "S", color = "red" } },
				new { price = 110.0m, stockQuantity = 5, attributes = new { size = "M", color = "red" } },
				new { price = 120.0m, stockQuantity = 3, attributes = new { size = "L", color = "blue" } }
			}
		};

		var resp = await _client.PostAsJsonAsync("/api/products", req);
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var productId = json.GetProperty("payload").GetGuid();
		productId.Should().NotBeEmpty();

		// Verify product has all 3 SKUs
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		getResp.StatusCode.Should().Be(HttpStatusCode.OK);
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var skus = productJson.GetProperty("payload").GetProperty("skus");
		skus.GetArrayLength().Should().Be(3);
	}

	[Fact]
	public async Task PostProducts_WithNoSkus_CreatesInactiveProduct()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var req = new
		{
			name = "Inactive Product",
			description = "Product without SKUs",
			categoryIds = new[] { categoryId }
		};

		var resp = await _client.PostAsJsonAsync("/api/products", req);
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var productId = json.GetProperty("payload").GetGuid();

		// Product should have a default SKU with price 0
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var skus = productJson.GetProperty("payload").GetProperty("skus");
		skus.GetArrayLength().Should().Be(1);
		skus.EnumerateArray().First().GetProperty("price").GetDecimal().Should().Be(0);
	}

	[Fact]
	public async Task PostProducts_WithSkuAttributes_StoresAttributesCorrectly()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var req = new
		{
			name = "Attribute Test Product",
			categoryIds = new[] { categoryId },
			skus = new[]
			{
				new
				{
					price = 50.0m,
					stockQuantity = 100,
					attributes = new
					{
						color = "navy blue",
						size = "XL",
						material = "cotton",
						weight = 0.5
					}
				}
			}
		};

		var resp = await _client.PostAsJsonAsync("/api/products", req);
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		var productId = json.GetProperty("payload").GetGuid();

		// Verify attributes are stored
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var firstSku = productJson.GetProperty("payload").GetProperty("skus").EnumerateArray().First();
		var attributes = firstSku.GetProperty("attributes");
		attributes.GetProperty("color").GetString().Should().Be("navy blue");
		attributes.GetProperty("size").GetString().Should().Be("XL");
	}

	#endregion

	#region SKU Management Tests

	[Fact]
	public async Task AddSku_ToExistingProduct_AddsNewSku()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		// Create product with one SKU
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Product For SKU Add",
			categoryIds = new[] { categoryId },
			skus = new[] { new { price = 50.0m, stockQuantity = 10 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		createResp.StatusCode.Should().Be(HttpStatusCode.OK);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Add another SKU
		var addSkuReq = new
		{
			price = 75.0m,
			stockQuantity = 5,
			attributes = new { variant = "premium" }
		};
		var addSkuResp = await _client.PostAsJsonAsync($"/api/products/{productId}/skus", addSkuReq);
		addSkuResp.StatusCode.Should().Be(HttpStatusCode.OK);

		var addSkuJson = await addSkuResp.Content.ReadFromJsonAsync<JsonElement>();
		addSkuJson.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var newSkuCode = addSkuJson.GetProperty("payload").GetString();
		newSkuCode.Should().NotBeNullOrWhiteSpace();

		// Verify product now has 2 SKUs
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		productJson.GetProperty("payload").GetProperty("skus").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public async Task DeleteSku_FromProduct_RemovesSku()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		// Create product with multiple SKUs (use unique attributes to avoid duplicate SkuCodes)
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Product For SKU Delete",
			categoryIds = new[] { categoryId },
			skus = new[]
			{
				new { price = 50.0m, stockQuantity = 10, attributes = new { variant = "standard" } },
				new { price = 75.0m, stockQuantity = 5, attributes = new { variant = "premium" } }
			}
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Get SKU IDs
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var skus = productJson.GetProperty("payload").GetProperty("skus").EnumerateArray().ToList();
		skus.Should().HaveCount(2);
		var skuIdToDelete = skus.First().GetProperty("id").GetGuid();

		// Delete one SKU
		var deleteResp = await _client.DeleteAsync($"/api/products/{productId}/skus/{skuIdToDelete}");
		deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify product now has 1 SKU
		var getAfterResp = await _client.GetAsync($"/api/products/{productId}");
		var productAfterJson = await getAfterResp.Content.ReadFromJsonAsync<JsonElement>();
		productAfterJson.GetProperty("payload").GetProperty("skus").GetArrayLength().Should().Be(1);
	}

	[Fact]
	public async Task AddSku_AsNonOwner_ReturnsForbidden()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (sellerEmail, sellerPassword) = await RegisterUser();
		var (otherEmail, otherPassword) = await RegisterUser();

		await EnsureUserInRole(sellerEmail, Roles.Seller);
		await EnsureUserInRole(otherEmail, Roles.Seller);

		var sellerToken = await LoginAndGetAccessToken(sellerEmail, sellerPassword);
		var otherSellerToken = await LoginAndGetAccessToken(otherEmail, otherPassword);

		var sellerUserId = await GetDomainUserIdByEmail(sellerEmail);
		var otherUserId = await GetDomainUserIdByEmail(otherEmail);
		await EnsureStoreForUser(sellerUserId);
		await EnsureStoreForUser(otherUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		// Create product as first seller
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Seller's Product",
			categoryIds = new[] { categoryId },
			skus = new[] { new { price = 50.0m, stockQuantity = 10 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Try to add SKU as different seller
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherSellerToken);
		var addSkuReq = new { price = 100.0m, stockQuantity = 5 };
		var addSkuResp = await _client.PostAsJsonAsync($"/api/products/{productId}/skus", addSkuReq);

		// Should fail because other seller doesn't own this product
		addSkuResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	#endregion

	#region Gallery Tests

	[Fact]
	public async Task UploadGalleryImage_ToProduct_UploadsSuccessfully()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		// Create product
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Product For Gallery",
			categoryIds = new[] { categoryId },
			skus = new[] { new { price = 50.0m, stockQuantity = 10 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Upload image
		using var imageContent = new ByteArrayContent(CreateTestImageBytes());
		imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
		using var formData = new MultipartFormDataContent();
		formData.Add(imageContent, "file", "test-image.png");
		formData.Add(new StringContent("0"), "displayOrder");

		var uploadResp = await _client.PostAsync($"/api/products/{productId}/gallery", formData);
		uploadResp.StatusCode.Should().Be(HttpStatusCode.OK);

		var uploadJson = await uploadResp.Content.ReadFromJsonAsync<JsonElement>();
		uploadJson.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		uploadJson.GetProperty("payload").GetProperty("galleryId").GetGuid().Should().NotBeEmpty();
		uploadJson.GetProperty("payload").GetProperty("url").GetString().Should().NotBeNullOrWhiteSpace();

		// Verify product gallery contains the image
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		productJson.GetProperty("payload").GetProperty("gallery").GetArrayLength().Should().Be(1);
	}

	[Fact]
	public async Task UploadMultipleGalleryImages_ToProduct_UploadsAllSuccessfully()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		// Create product
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Product With Multiple Images",
			categoryIds = new[] { categoryId },
			skus = new[] { new { price = 50.0m, stockQuantity = 10 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Upload 3 images
		for (int i = 0; i < 3; i++)
		{
			using var imageContent = new ByteArrayContent(CreateTestImageBytes());
			imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
			using var formData = new MultipartFormDataContent();
			formData.Add(imageContent, "file", $"test-image-{i}.png");
			formData.Add(new StringContent(i.ToString()), "displayOrder");

			var uploadResp = await _client.PostAsync($"/api/products/{productId}/gallery", formData);
			uploadResp.StatusCode.Should().Be(HttpStatusCode.OK);
		}

		// Verify product gallery contains all 3 images
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		productJson.GetProperty("payload").GetProperty("gallery").GetArrayLength().Should().Be(3);
	}

	[Fact]
	public async Task DeleteGalleryImage_FromProduct_RemovesImage()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		// Create product
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Product For Gallery Delete",
			categoryIds = new[] { categoryId },
			skus = new[] { new { price = 50.0m, stockQuantity = 10 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Upload image
		using var imageContent = new ByteArrayContent(CreateTestImageBytes());
		imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
		using var formData = new MultipartFormDataContent();
		formData.Add(imageContent, "file", "test-image.png");

		var uploadResp = await _client.PostAsync($"/api/products/{productId}/gallery", formData);
		var uploadJson = await uploadResp.Content.ReadFromJsonAsync<JsonElement>();
		var galleryId = uploadJson.GetProperty("payload").GetProperty("galleryId").GetGuid();

		// Delete the image
		var deleteResp = await _client.DeleteAsync($"/api/products/{productId}/gallery/{galleryId}");
		deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify gallery is empty
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		productJson.GetProperty("payload").GetProperty("gallery").GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task UploadGalleryImage_WithoutFile_ReturnsBadRequest()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (email, password) = await RegisterUser();

		await EnsureUserInRole(email, Roles.Seller);
		var sellerToken = await LoginAndGetAccessToken(email, password);
		var sellerUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(sellerUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		// Create product
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Product For Empty Upload",
			categoryIds = new[] { categoryId },
			skus = new[] { new { price = 50.0m, stockQuantity = 10 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Try to upload without file
		using var formData = new MultipartFormDataContent();
		formData.Add(new StringContent("0"), "displayOrder");

		var uploadResp = await _client.PostAsync($"/api/products/{productId}/gallery", formData);
		uploadResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UploadGalleryImage_AsNonOwner_ReturnsForbidden()
	{
		var adminToken = await LoginAdminAndGetAccessToken();
		var (sellerEmail, sellerPassword) = await RegisterUser();
		var (otherEmail, otherPassword) = await RegisterUser();

		await EnsureUserInRole(sellerEmail, Roles.Seller);
		await EnsureUserInRole(otherEmail, Roles.Seller);

		var sellerToken = await LoginAndGetAccessToken(sellerEmail, sellerPassword);
		var otherSellerToken = await LoginAndGetAccessToken(otherEmail, otherPassword);

		var sellerUserId = await GetDomainUserIdByEmail(sellerEmail);
		var otherUserId = await GetDomainUserIdByEmail(otherEmail);
		await EnsureStoreForUser(sellerUserId);
		await EnsureStoreForUser(otherUserId);

		var categoryId = await CreateCategoryAsAdmin(adminToken);

		// Create product as first seller
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Seller's Product For Gallery",
			categoryIds = new[] { categoryId },
			skus = new[] { new { price = 50.0m, stockQuantity = 10 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Try to upload image as different seller
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherSellerToken);
		using var imageContent = new ByteArrayContent(CreateTestImageBytes());
		imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
		using var formData = new MultipartFormDataContent();
		formData.Add(imageContent, "file", "test-image.png");

		var uploadResp = await _client.PostAsync($"/api/products/{productId}/gallery", formData);

		// Should fail because other seller doesn't own this product
		uploadResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	/// <summary>
	/// Creates minimal valid PNG bytes for testing
	/// </summary>
	private static byte[] CreateTestImageBytes()
	{
		// Minimal 1x1 PNG (red pixel)
		return new byte[]
		{
			0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
			0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
			0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
			0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
			0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
			0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
			0x00, 0x00, 0x03, 0x00, 0x01, 0x00, 0x05, 0xFE,
			0xD4, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, // IEND chunk
			0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
		};
	}

	#endregion

	#region Product Categories Management Tests

	[Fact]
	public async Task UpdateProduct_CanAddCategories_Success()
	{
		// Arrange
		var (email, password) = await RegisterUser();
		await EnsureUserInRole(email, Roles.Seller);
		var domainUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(domainUserId);
		var sellerToken = await LoginAndGetAccessToken(email, password);

		// Create categories
		var adminToken = await LoginAdminAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var cat1Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Category 1" });
		var cat1Id = (await cat1Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat2Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Category 2" });
		var cat2Id = (await cat2Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat3Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Category 3" });
		var cat3Id = (await cat3Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Create product with one category
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Test Product",
			description = "Test description",
			categoryIds = new[] { cat1Id },
			skus = new[] { new { price = 100.0m, stockQuantity = 10 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		createResp.EnsureSuccessStatusCode();
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Act - Add more categories
		var updateReq = new
		{
			name = "Test Product",
			description = "Test description",
			categoryIds = new[] { cat1Id, cat2Id, cat3Id }
		};
		var updateResp = await _client.PutAsJsonAsync($"/api/products/{productId}", updateReq);

		// Assert - read response body for debugging
		var responseContent = await updateResp.Content.ReadAsStringAsync();
		Assert.True(updateResp.IsSuccessStatusCode, $"Expected success but got {updateResp.StatusCode}: {responseContent}");

		// Verify categories were added
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		getResp.EnsureSuccessStatusCode();
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var categories = productJson.GetProperty("payload").GetProperty("categories");

		categories.GetArrayLength().Should().Be(3);
		var categoryIds = categories.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ToList();
		categoryIds.Should().Contain(cat1Id);
		categoryIds.Should().Contain(cat2Id);
		categoryIds.Should().Contain(cat3Id);
	}

	[Fact]
	public async Task UpdateProduct_CanRemoveCategories_Success()
	{
		// Arrange
		var (email, password) = await RegisterUser();
		await EnsureUserInRole(email, Roles.Seller);
		var domainUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(domainUserId);
		var sellerToken = await LoginAndGetAccessToken(email, password);

		// Create categories
		var adminToken = await LoginAdminAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var cat1Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Category A" });
		var cat1Id = (await cat1Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat2Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Category B" });
		var cat2Id = (await cat2Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat3Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Category C" });
		var cat3Id = (await cat3Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Create product with three categories
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Multi-Category Product",
			description = "Test description",
			categoryIds = new[] { cat1Id, cat2Id, cat3Id },
			skus = new[] { new { price = 50.0m, stockQuantity = 5 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		createResp.EnsureSuccessStatusCode();
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Act - Remove one category (keep only cat1)
		var updateReq = new
		{
			name = "Multi-Category Product",
			description = "Test description",
			categoryIds = new[] { cat1Id }
		};
		var updateResp = await _client.PutAsJsonAsync($"/api/products/{productId}", updateReq);

		// Assert
		updateResp.EnsureSuccessStatusCode();

		// Verify only one category remains
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		getResp.EnsureSuccessStatusCode();
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var categories = productJson.GetProperty("payload").GetProperty("categories");

		categories.GetArrayLength().Should().Be(1);
		var categoryId = categories[0].GetProperty("id").GetGuid();
		categoryId.Should().Be(cat1Id);
	}

	[Fact]
	public async Task UpdateProduct_CanSetPrimaryCategory_Success()
	{
		// Arrange
		var (email, password) = await RegisterUser();
		await EnsureUserInRole(email, Roles.Seller);
		var domainUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(domainUserId);
		var sellerToken = await LoginAndGetAccessToken(email, password);

		// Create categories with unique names to avoid collisions with seeded data
		var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
		var adminToken = await LoginAdminAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var cat1Resp = await _client.PostAsJsonAsync("/api/categories", new { name = $"PrimaryTestCat_{uniqueSuffix}" });
		cat1Resp.EnsureSuccessStatusCode();
		var cat1Id = (await cat1Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat2Resp = await _client.PostAsJsonAsync("/api/categories", new { name = $"SecondaryTestCat_{uniqueSuffix}" });
		cat2Resp.EnsureSuccessStatusCode();
		var cat2Id = (await cat2Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Create product
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Laptop",
			description = "Test laptop description",
			categoryIds = new[] { cat1Id, cat2Id },
			skus = new[] { new { price = 1000.0m, stockQuantity = 5 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		createResp.EnsureSuccessStatusCode();
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Act - Set cat2 as primary category
		var updateReq = new
		{
			name = "Laptop",
			description = "Test laptop description",
			categoryIds = new[] { cat1Id, cat2Id },
			primaryCategoryId = cat2Id
		};
		var updateResp = await _client.PutAsJsonAsync($"/api/products/{productId}", updateReq);

		// Assert
		updateResp.EnsureSuccessStatusCode();

		// Verify primary category is set correctly
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		getResp.EnsureSuccessStatusCode();
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var payload = productJson.GetProperty("payload");

		// Check primaryCategory field
		var primaryCategory = payload.GetProperty("primaryCategory");
		primaryCategory.GetProperty("id").GetGuid().Should().Be(cat2Id);
		// Name should match our unique test category
		primaryCategory.GetProperty("name").GetString().Should().StartWith("SecondaryTestCat_");

		// Check that categories contain isPrimary flag
		var categories = payload.GetProperty("categories");
		var cat2InList = categories.EnumerateArray()
			.FirstOrDefault(c => c.GetProperty("id").GetGuid() == cat2Id);

		cat2InList.GetProperty("isPrimary").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public async Task UpdateProduct_ChangePrimaryCategory_Success()
	{
		// Arrange
		var (email, password) = await RegisterUser();
		await EnsureUserInRole(email, Roles.Seller);
		var domainUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(domainUserId);
		var sellerToken = await LoginAndGetAccessToken(email, password);

		// Create categories
		var adminToken = await LoginAdminAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var cat1Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Books" });
		var cat1Id = (await cat1Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat2Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Fiction" });
		var cat2Id = (await cat2Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Create product with cat1 as primary (first category)
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Novel",
			description = "A great novel",
			categoryIds = new[] { cat1Id, cat2Id },
			skus = new[] { new { price = 20.0m, stockQuantity = 100 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		createResp.EnsureSuccessStatusCode();
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Act - Change primary to cat2
		var updateReq = new
		{
			name = "Novel",
			description = "A great novel",
			categoryIds = new[] { cat1Id, cat2Id },
			primaryCategoryId = cat2Id
		};
		var updateResp = await _client.PutAsJsonAsync($"/api/products/{productId}", updateReq);

		// Assert
		updateResp.EnsureSuccessStatusCode();

		// Verify primary category changed
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		getResp.EnsureSuccessStatusCode();
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var payload = productJson.GetProperty("payload");

		var primaryCategory = payload.GetProperty("primaryCategory");
		primaryCategory.GetProperty("id").GetGuid().Should().Be(cat2Id);

		// Verify only one category is marked as primary
		var categories = payload.GetProperty("categories");
		var primaryCount = categories.EnumerateArray()
			.Count(c => c.TryGetProperty("isPrimary", out var isPrimary) && isPrimary.GetBoolean());

		primaryCount.Should().Be(1, "Only one category should be marked as primary");
	}

	[Fact]
	public async Task UpdateProduct_PrimaryCategoryNotInList_ReturnsBadRequest()
	{
		// Arrange
		var (email, password) = await RegisterUser();
		await EnsureUserInRole(email, Roles.Seller);
		var domainUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(domainUserId);
		var sellerToken = await LoginAndGetAccessToken(email, password);

		// Create categories
		var adminToken = await LoginAdminAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var cat1Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Cat1" });
		var cat1Id = (await cat1Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat2Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Cat2" });
		var cat2Id = (await cat2Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Create product
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Test",
			description = "Test product",
			categoryIds = new[] { cat1Id },
			skus = new[] { new { price = 10.0m, stockQuantity = 1 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		createResp.EnsureSuccessStatusCode();
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Act - Try to set cat2 as primary, but it's not in categoryIds
		var updateReq = new
		{
			name = "Test",
			description = "Test product",
			categoryIds = new[] { cat1Id },
			primaryCategoryId = cat2Id // Not in categoryIds!
		};
		var updateResp = await _client.PutAsJsonAsync($"/api/products/{productId}", updateReq);

		// Assert
		updateResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var errorJson = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
		errorJson.GetProperty("message").GetString()
			.Should().Contain("Primary category must be one of the selected categories");
	}

	[Fact]
	public async Task UpdateProduct_AddAndRemoveCategoriesSimultaneously_Success()
	{
		// Arrange
		var (email, password) = await RegisterUser();
		await EnsureUserInRole(email, Roles.Seller);
		var domainUserId = await GetDomainUserIdByEmail(email);
		await EnsureStoreForUser(domainUserId);
		var sellerToken = await LoginAndGetAccessToken(email, password);

		// Create categories
		var adminToken = await LoginAdminAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var cat1Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Old Cat 1" });
		var cat1Id = (await cat1Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat2Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "Old Cat 2" });
		var cat2Id = (await cat2Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat3Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "New Cat 3" });
		var cat3Id = (await cat3Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		var cat4Resp = await _client.PostAsJsonAsync("/api/categories", new { name = "New Cat 4" });
		var cat4Id = (await cat4Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Create product with cat1 and cat2
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
		var createReq = new
		{
			name = "Dynamic Categories Product",
			description = "Product with dynamic categories",
			categoryIds = new[] { cat1Id, cat2Id },
			skus = new[] { new { price = 75.0m, stockQuantity = 20 } }
		};
		var createResp = await _client.PostAsJsonAsync("/api/products", createReq);
		createResp.EnsureSuccessStatusCode();
		var productId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("payload").GetGuid();

		// Act - Replace with cat2, cat3, cat4 (remove cat1, keep cat2, add cat3 and cat4)
		var updateReq = new
		{
			name = "Dynamic Categories Product",
			description = "Product with dynamic categories",
			categoryIds = new[] { cat2Id, cat3Id, cat4Id }
		};
		var updateResp = await _client.PutAsJsonAsync($"/api/products/{productId}", updateReq);

		// Assert
		updateResp.EnsureSuccessStatusCode();

		// Verify correct categories
		var getResp = await _client.GetAsync($"/api/products/{productId}");
		getResp.EnsureSuccessStatusCode();
		var productJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
		var categories = productJson.GetProperty("payload").GetProperty("categories");

		categories.GetArrayLength().Should().Be(3);
		var categoryIds = categories.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ToList();

		categoryIds.Should().NotContain(cat1Id, "cat1 should be removed");
		categoryIds.Should().Contain(cat2Id, "cat2 should be kept");
		categoryIds.Should().Contain(cat3Id, "cat3 should be added");
		categoryIds.Should().Contain(cat4Id, "cat4 should be added");
	}

	#endregion
}
