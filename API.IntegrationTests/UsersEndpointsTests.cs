using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using System.Threading.Tasks;

namespace API.IntegrationTests;

public class UsersEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
	private readonly HttpClient _client;

	public UsersEndpointsTests(TestWebApplicationFactory factory)
	{
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

	private async Task<(string Email, string Password, string AccessToken)> RegisterUserAndGetAccessToken()
	{
		var email = $"test_{Guid.NewGuid():N}@example.com";
		var password = "User123!";
		var reg = new { email, name = "Test", surname = "User", password, confirmPassword = password };
		var resp = await _client.PostAsJsonAsync("/api/auth/register", reg);
		resp.EnsureSuccessStatusCode();
		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		var token = json.GetProperty("accessToken").GetString()!;
		return (email, password, token);
	}

	[Fact]
	public async Task GetMyProfile_WithValidToken_ReturnsUserDto()
	{
		var token = await LoginAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var resp = await _client.GetAsync("/api/users/me");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.TryGetProperty("isSuccess", out var isSuccess).Should().BeTrue();
		isSuccess.GetBoolean().Should().BeTrue();

		json.TryGetProperty("payload", out var payload).Should().BeTrue();
		payload.TryGetProperty("username", out var username).Should().BeTrue();
		payload.TryGetProperty("email", out var email).Should().BeTrue();
		payload.TryGetProperty("roles", out var roles).Should().BeTrue();

		username.GetString().Should().NotBeNullOrWhiteSpace();
		email.GetString().Should().Be("admin@example.com");
		roles.ValueKind.Should().Be(JsonValueKind.Array);
	}

	[Fact]
	public async Task GetUsers_AsAdmin_ReturnsList()
	{
		var token = await LoginAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var resp = await _client.GetAsync("/api/users");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var payload = json.GetProperty("payload");
		payload.ValueKind.Should().Be(JsonValueKind.Array);
		payload.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
		var foundAdmin = false;
		foreach (var e in payload.EnumerateArray())
		{
			if (e.TryGetProperty("email", out var email) && email.GetString() == "admin@example.com")
			{
				foundAdmin = true;
				break;
			}
		}
		foundAdmin.Should().BeTrue();
	}

	[Fact]
	public async Task GetUsers_AsRegularUser_Returns403()
	{
		var (_, _, token) = await RegisterUserAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var resp = await _client.GetAsync("/api/users");
		resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task GetUserByEmail_ReturnsUser()
	{
		var token = await LoginAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var resp = await _client.GetAsync("/api/users/by-email/admin@example.com");
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		json.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
		var payload = json.GetProperty("payload");
		payload.GetProperty("email").GetString().Should().Be("admin@example.com");
	}

	[Fact]
	public async Task GetMyProfile_WithoutToken_Returns401()
	{
		_client.DefaultRequestHeaders.Authorization = null;
		var resp = await _client.GetAsync("/api/users/me");
		resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ChangePassword_ThenLoginWithNewPassword_Succeeds()
	{
		// Register a fresh user and use the returned token
		var (email, currentPassword, token) = await RegisterUserAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		// Change password
		var change = new { currentPassword = currentPassword, newPassword = "User123!X" };
		var changeResp = await _client.PutAsJsonAsync("/api/users/me/password", change);
		changeResp.StatusCode.Should().Be(HttpStatusCode.OK);

		// Old password should fail
		var oldLogin = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = currentPassword });
		oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

		// New password should succeed
		var newCreds = new { email, password = "User123!X" };
		var newLogin = await _client.PostAsJsonAsync("/api/auth/login", newCreds);
		newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task DeleteUser_AsAdmin_DeletesUser()
	{
		// Create a temp user
		var (tempEmail, _, _) = await RegisterUserAndGetAccessToken();

		// Get admin token
		var adminToken = await LoginAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		// Find user by email to get ID
		var userResp = await _client.GetAsync($"/api/users/by-email/{tempEmail}");
		userResp.StatusCode.Should().Be(HttpStatusCode.OK);
		var userJson = await userResp.Content.ReadFromJsonAsync<JsonElement>();
		var userId = userJson.GetProperty("payload").GetProperty("id").GetGuid();

		// Delete user
		var deleteResp = await _client.DeleteAsync($"/api/users/{userId}");
		deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify user no longer exists
		var checkResp = await _client.GetAsync($"/api/users/by-email/{tempEmail}");
		var checkJson = await checkResp.Content.ReadFromJsonAsync<JsonElement>();
		checkJson.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public async Task GetUserById_AsRegularUser_Returns403()
	{
		var (_, _, token) = await RegisterUserAndGetAccessToken();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var resp = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");
		resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}
}
