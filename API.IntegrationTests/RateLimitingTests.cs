using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace API.IntegrationTests;

// Isolated collection to prevent parallel execution with other tests
[Collection("RateLimiting")]
public class RateLimitingTests : IClassFixture<RateLimitingTestWebApplicationFactory>
{
	private readonly HttpClient _client;

	public RateLimitingTests(RateLimitingTestWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task Exceeding_GlobalLimit_Returns429()
	{
		// PermitLimit=2 per 1 second (configured in TestWebApplicationFactory)
		var first = await _client.GetAsync("/api/auth/check-email?email=admin@example.com");
		first.StatusCode.Should().Be(HttpStatusCode.OK);

		var second = await _client.GetAsync("/api/auth/check-email?email=admin@example.com");
		second.StatusCode.Should().Be(HttpStatusCode.OK);

		var third = await _client.GetAsync("/api/auth/check-email?email=admin@example.com");
		third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

		var body = await third.Content.ReadAsStringAsync();
		body.Should().Contain("Too many requests");

		if (third.Headers.RetryAfter is not null)
		{
			third.Headers.RetryAfter.Delta.Should().NotBeNull();
		}
	}
}

public class RateLimitingTestWebApplicationFactory : TestWebApplicationFactory
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		base.ConfigureWebHost(builder);

		builder.ConfigureAppConfiguration((ctx, cfg) =>
		{
			var dict = new Dictionary<string, string?>
			{
				// Tight limit: 2 requests per second, no queue
				["RateLimiting:Enabled"] = "true",
				["RateLimiting:PartitionByIp"] = "false",
				["RateLimiting:PermitLimit"] = "2",
				["RateLimiting:WindowInSeconds"] = "1",
				["RateLimiting:QueueLimit"] = "0",
				["RateLimiting:QueueProcessingOrder"] = "OldestFirst",
				["RateLimiting:RejectionStatusCode"] = "429"
			};

			cfg.AddInMemoryCollection(dict);
		});
	}
}
