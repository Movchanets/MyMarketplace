using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure;
using Infrastructure.Initializer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
		.WithImage("postgres:16-alpine")
		.WithDatabase("testdb")
		.WithUsername("testuser")
		.WithPassword("testpass")
		.Build();

	public async Task InitializeAsync()
	{
		await _postgresContainer.StartAsync();

		// Create the server to trigger app initialization
		_ = Server;

		// Run migrations after the app is built
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync();

		// Seed test data using SeederDB from Infrastructure
		// Create a minimal IApplicationBuilder wrapper to call SeedDataAsync
		var appBuilder = new ApplicationBuilderWrapper(Services);
		await SeederDB.SeedDataAsync(appBuilder);
	}

	public new async Task DisposeAsync()
	{
		await _postgresContainer.DisposeAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureAppConfiguration((ctx, cfg) =>
		{
			var dict = new Dictionary<string, string?>
			{
				["JwtSettings:AccessTokenSecret"] = "test_secret_12345678901234567890",
				["JwtSettings:Issuer"] = "TestIssuer",
				["JwtSettings:Audience"] = "TestAudience",
				["JwtSettings:AccessTokenExpirationMinutes"] = "60",
				["ConnectionStrings:NeonConnection"] = _postgresContainer.GetConnectionString(),
				// Disable rate limiting for most tests to avoid interference
				["RateLimiting:Enabled"] = "false"
			};
			cfg.AddInMemoryCollection(dict);
		});

		builder.ConfigureServices(services =>
		{
			// Replace DbContext with PostgreSQL Testcontainer
			var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
			if (descriptor is not null)
			{
				services.Remove(descriptor);
			}

			services.AddDbContext<AppDbContext>(options =>
			{
				options.UseNpgsql(_postgresContainer.GetConnectionString());
			});
		});
	}
}

// Minimal IApplicationBuilder wrapper for testing
internal class ApplicationBuilderWrapper : Microsoft.AspNetCore.Builder.IApplicationBuilder
{
	public ApplicationBuilderWrapper(IServiceProvider serviceProvider)
	{
		ApplicationServices = serviceProvider;
		Properties = new Dictionary<string, object?>();
		ServerFeatures = new Microsoft.AspNetCore.Http.Features.FeatureCollection();
	}

	public IServiceProvider ApplicationServices { get; set; }
	public IDictionary<string, object?> Properties { get; }
	public Microsoft.AspNetCore.Http.Features.IFeatureCollection ServerFeatures { get; }

	public Microsoft.AspNetCore.Http.RequestDelegate Build()
	{
		throw new NotImplementedException();
	}

	public Microsoft.AspNetCore.Builder.IApplicationBuilder New()
	{
		return new ApplicationBuilderWrapper(ApplicationServices);
	}

	public Microsoft.AspNetCore.Builder.IApplicationBuilder Use(Func<Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.AspNetCore.Http.RequestDelegate> middleware)
	{
		return this;
	}
}
