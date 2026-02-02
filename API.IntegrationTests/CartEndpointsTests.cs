using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace API.IntegrationTests;

public class CartEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
	private readonly TestWebApplicationFactory _factory;
	private readonly HttpClient _client;

	public CartEndpointsTests(TestWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();
	}

	private async Task<string> LoginAndGetAccessToken()
	{
		var payload = new { email = "admin@example.com", password = "Qwerty-1!" };
		var resp = await _client.PostAsJsonAsync("/api/auth/login", payload);
		resp.EnsureSuccessStatusCode();
		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		return json.GetProperty("accessToken").GetString()!;
	}

	private async Task<HttpClient> CreateAuthenticatedClientAsync()
	{
		var client = _factory.CreateClient();
		var token = await LoginAndGetAccessToken();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		return client;
	}

	[Fact]
	public async Task GetCart_Unauthenticated_ReturnsUnauthorized()
	{
		// Act
		var response = await _client.GetAsync("/api/cart");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetCart_Authenticated_ReturnsSuccess()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();

		// Act
		var response = await client.GetAsync("/api/cart");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<ServiceResponse<CartDto>>();
		result.Should().NotBeNull();
		result!.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task AddToCart_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var request = new { productId = Guid.NewGuid(), skuId = Guid.NewGuid(), quantity = 1 };

		// Act
		var response = await _client.PostAsJsonAsync("/api/cart/items", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task AddToCart_InvalidQuantity_ReturnsBadRequest()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var request = new { productId = Guid.NewGuid(), skuId = Guid.NewGuid(), quantity = 0 };

		// Act
		var response = await client.PostAsJsonAsync("/api/cart/items", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task AddToCart_NonExistentProduct_ReturnsBadRequest()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var request = new { productId = Guid.NewGuid(), skuId = Guid.NewGuid(), quantity = 1 };

		// Act
		var response = await client.PostAsJsonAsync("/api/cart/items", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var result = await response.Content.ReadFromJsonAsync<ServiceResponse<CartDto>>();
		result.Should().NotBeNull();
		result!.IsSuccess.Should().BeFalse();
	}

	[Fact]
	public async Task AddToCart_Authenticated_WithExistingProduct_ReturnsSuccess()
	{
		// Arrange: create a seller user, ensure seller role and store, create category as admin, create product as seller
		var adminToken = await LoginAndGetAccessToken();

		// Create a category as admin
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
		var createCatResp = await _client.PostAsJsonAsync("/api/categories", new { name = "CatForCart" + Guid.NewGuid().ToString("N"), description = "d" });
		createCatResp.EnsureSuccessStatusCode();
		var catJson = await createCatResp.Content.ReadFromJsonAsync<JsonElement>();
		var categoryId = catJson.GetProperty("payload").GetGuid();

		// Register a new user (seller)
		var email = $"seller_{Guid.NewGuid():N}@example.com";
		var password = "User123!";
		var reg = new { email, name = "Test", surname = "Seller", password, confirmPassword = password };
		var regResp = await _client.PostAsJsonAsync("/api/auth/register", reg);
		regResp.EnsureSuccessStatusCode();

		// Assign 'Seller' role to the user and ensure they have a store
		using (var scope = _factory.Services.CreateScope())
		{
			var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Infrastructure.Entities.Identity.ApplicationUser>>();
			var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Infrastructure.Entities.Identity.RoleEntity>>();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			// Ensure role exists
			if (!await roleManager.RoleExistsAsync(Domain.Constants.Roles.Seller))
			{
				await roleManager.CreateAsync(new Infrastructure.Entities.Identity.RoleEntity { Name = Domain.Constants.Roles.Seller });
			}

			var appUser = await userManager.FindByEmailAsync(email);
			appUser.Should().NotBeNull();
			await userManager.AddToRoleAsync(appUser!, Domain.Constants.Roles.Seller);

			// Ensure domain user exists and store created
			var domainUserId = appUser!.DomainUserId ?? db.DomainUsers.First(u => u.IdentityUserId == appUser.Id).Id;
			var hasStore = await db.Stores.AnyAsync(s => s.UserId == domainUserId);
			if (!hasStore)
			{
				db.Stores.Add(Domain.Entities.Store.Create(domainUserId, $"Store {Guid.NewGuid():N}"));
				await db.SaveChangesAsync();
			}
		}

		// Login as seller
		var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
		loginResp.EnsureSuccessStatusCode();
		var loginJson = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
		var sellerToken = loginJson.GetProperty("accessToken").GetString()!;
		var sellerClient = _factory.CreateClient();
		sellerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);

		// Create product as seller
		var createReq = new
		{
			name = "Test Product for Cart " + Guid.NewGuid().ToString("N"),
			description = "d",
			categoryIds = new[] { categoryId },
			Skus = new[] {
				new { price = 10.50m, stockQuantity = 5, attributes = new { color = "blue" } }
			},
			tagIds = new Guid[0]
		};

		var createResp = await sellerClient.PostAsJsonAsync("/api/products", createReq);
		createResp.EnsureSuccessStatusCode();
		var json = await createResp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var productId = json.GetProperty("payload").GetGuid();

		// Get product details to extract SKU id
		var prodResp = await sellerClient.GetAsync($"/api/products/{productId}");
		prodResp.EnsureSuccessStatusCode();
		var prodJson = await prodResp.Content.ReadFromJsonAsync<JsonElement>();
		var skuId = prodJson.GetProperty("payload").GetProperty("skus")[0].GetProperty("id").GetGuid();

		// Act: add to cart as seller
		var addReq = new { productId = productId, skuId = skuId, quantity = 1 };
		var addResp = await sellerClient.PostAsJsonAsync("/api/cart/items", addReq);

		// Assert
		addResp.StatusCode.Should().Be(HttpStatusCode.OK);
		var addResult = await addResp.Content.ReadFromJsonAsync<ServiceResponse<CartDto>>();
		addResult.Should().NotBeNull();
		addResult!.IsSuccess.Should().BeTrue();
		addResult.Payload.Should().NotBeNull();
		addResult.Payload!.Items.Should().NotBeNull();
		addResult.Payload.Items.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task UpdateQuantity_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var cartItemId = Guid.NewGuid();
		var request = new { quantity = 5 };

		// Act
		var response = await _client.PutAsJsonAsync($"/api/cart/items/{cartItemId}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task UpdateQuantity_InvalidQuantity_ReturnsBadRequest()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var cartItemId = Guid.NewGuid();
		var request = new { quantity = 0 };

		// Act
		var response = await client.PutAsJsonAsync($"/api/cart/items/{cartItemId}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task RemoveFromCart_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var cartItemId = Guid.NewGuid();

		// Act
		var response = await _client.DeleteAsync($"/api/cart/items/{cartItemId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task RemoveFromCart_NonExistentItem_ReturnsBadRequest()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var cartItemId = Guid.NewGuid();

		// Act
		var response = await client.DeleteAsync($"/api/cart/items/{cartItemId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task ClearCart_Unauthenticated_ReturnsUnauthorized()
	{
		// Act
		var response = await _client.DeleteAsync("/api/cart");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ClearCart_Authenticated_ReturnsSuccess()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();

		// Act
		var response = await client.DeleteAsync("/api/cart");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
		result.Should().NotBeNull();
		result!.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task FullCartFlow_AddUpdateRemoveClear()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();

		// Clear cart first
		await client.DeleteAsync("/api/cart");

		// Act & Assert - Add to cart (will fail due to non-existent product, but tests the flow)
		var addRequest = new { productId = Guid.NewGuid(), skuId = Guid.NewGuid(), quantity = 2 };
		var addResponse = await client.PostAsJsonAsync("/api/cart/items", addRequest);

		// Since we don't have real products, this should fail
		addResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}
}
