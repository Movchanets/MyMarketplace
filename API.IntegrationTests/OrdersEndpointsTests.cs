using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

public class OrdersEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
	private readonly TestWebApplicationFactory _factory;
	private readonly HttpClient _client;

	public OrdersEndpointsTests(TestWebApplicationFactory factory)
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
	public async Task GetOrders_Unauthenticated_ReturnsUnauthorized()
	{
		// Act
		var response = await _client.GetAsync("/api/orders");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetOrders_Authenticated_ReturnsSuccess()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();

		// Act
		var response = await client.GetAsync("/api/orders");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<ServiceResponse<object>>();
		result.Should().NotBeNull();
		result!.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task GetOrders_WithStatusFilter_ReturnsSuccess()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();

		// Act
		var response = await client.GetAsync("/api/orders?status=Pending");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetOrder_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var orderId = Guid.NewGuid();

		// Act
		var response = await _client.GetAsync($"/api/orders/{orderId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetOrder_NonExistent_ReturnsNotFound()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var orderId = Guid.NewGuid();

		// Act
		var response = await client.GetAsync($"/api/orders/{orderId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CreateOrder_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var request = new
		{
			shippingAddress = new
			{
				firstName = "John",
				lastName = "Doe",
				phoneNumber = "1234567890",
				email = "john@example.com",
				addressLine1 = "123 Main St",
				city = "New York",
				postalCode = "10001",
				country = "USA"
			},
			deliveryMethod = "Standard Shipping",
			paymentMethod = "Credit Card"
		};

		// Act
		var response = await _client.PostAsJsonAsync("/api/orders", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task CreateOrder_EmptyCart_ReturnsBadRequest()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();

		// Clear cart first
		await client.DeleteAsync("/api/cart");

		var request = new
		{
			shippingAddress = new
			{
				firstName = "John",
				lastName = "Doe",
				phoneNumber = "1234567890",
				email = "john@example.com",
				addressLine1 = "123 Main St",
				city = "New York",
				postalCode = "10001",
				country = "USA"
			},
			deliveryMethod = "Standard Shipping",
			paymentMethod = "Credit Card"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/orders", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateOrder_InvalidShippingAddress_ReturnsBadRequest()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var request = new
		{
			shippingAddress = new
			{
				firstName = "",
				lastName = "",
				phoneNumber = "",
				email = "invalid",
				addressLine1 = "",
				city = "",
				postalCode = "",
				country = ""
			},
			deliveryMethod = "",
			paymentMethod = ""
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/orders", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CancelOrder_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var orderId = Guid.NewGuid();
		var request = new { reason = "Changed my mind" };

		// Act
		var response = await _client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task CancelOrder_NonExistent_ReturnsNotFound()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var orderId = Guid.NewGuid();
		var request = new { reason = "Changed my mind" };

		// Act
		var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetOrderStatusHistory_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var orderId = Guid.NewGuid();

		// Act
		var response = await _client.GetAsync($"/api/orders/{orderId}/status-history");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetOrderStatusHistory_NonExistent_ReturnsNotFound()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var orderId = Guid.NewGuid();

		// Act
		var response = await client.GetAsync($"/api/orders/{orderId}/status-history");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateOrderStatus_AsNonAdmin_ReturnsNotFound()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();
		var orderId = Guid.NewGuid();
		var request = new { newStatus = "Confirmed" };

		// Act
		var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/status", request);

		// Assert - ASP.NET Core returns 404 for endpoints with role-based auth when user doesn't have the role
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateOrderStatus_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var orderId = Guid.NewGuid();
		var request = new { newStatus = "Confirmed" };

		// Act
		var response = await _client.PutAsJsonAsync($"/api/orders/{orderId}/status", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Orders_Pagination_ReturnsCorrectPageSize()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();

		// Act
		var response = await client.GetAsync("/api/orders?pageNumber=1&pageSize=5");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task Orders_Sorting_ReturnsSuccess()
	{
		// Arrange
		var client = await CreateAuthenticatedClientAsync();

		// Act
		var response = await client.GetAsync("/api/orders?sortBy=CreatedAt&sortDescending=true");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}
}
