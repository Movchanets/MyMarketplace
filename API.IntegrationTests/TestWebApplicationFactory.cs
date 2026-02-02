using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Infrastructure;
using Infrastructure.Initializer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests;

/// <summary>
/// Test factory that provides a PostgreSQL test container and mocks for external services.
/// MassTransit is disabled in Testing environment to avoid circular dependencies.
/// </summary>
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
				["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
				["ConnectionStrings:NeonConnection"] = _postgresContainer.GetConnectionString(),
				["RateLimiting:Enabled"] = "false",
				["SmtpSettings:Host"] = "localhost",
				["SmtpSettings:Port"] = "587",
				["SmtpSettings:Username"] = "test@example.com",
				["SmtpSettings:Password"] = "testpass",
				["SmtpSettings:From"] = "noreply@example.com",
				["SmtpSettings:EnableSsl"] = "false"
			};
			cfg.AddInMemoryCollection(dict);
		});

		builder.ConfigureServices((context, services) =>
		{
			// Replace DbContext with PostgreSQL Testcontainer
			var dbContextDescriptor = services.SingleOrDefault(d => 
				d.ServiceType == typeof(DbContextOptions<AppDbContext>));
			if (dbContextDescriptor is not null)
			{
				services.Remove(dbContextDescriptor);
			}

			services.AddDbContext<AppDbContext>(options =>
			{
				options.UseNpgsql(_postgresContainer.GetConnectionString());
			});

			// Replace IEmailService with mock
			// (MassTransit is disabled in Testing environment per Program.cs)
			var emailServiceDescriptor = services.SingleOrDefault(d => 
				d.ServiceType == typeof(IEmailService));
			if (emailServiceDescriptor is not null)
			{
				services.Remove(emailServiceDescriptor);
			}

			services.AddSingleton<IEmailService>(sp => CreateMockEmailService(sp));
		});
	}

	private static IEmailService CreateMockEmailService(IServiceProvider sp)
	{
		var mock = new Mock<IEmailService>();
		var logger = sp.GetRequiredService<ILogger<TestWebApplicationFactory>>();
		
		mock.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
			.Callback<string, string>((email, url) => 
				logger.LogDebug("Mock: Password reset email to {Email}", email))
			.Returns(Task.CompletedTask);
			
		mock.Setup(x => x.SendTemporaryPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
			.Callback<string, string>((email, pass) => 
				logger.LogDebug("Mock: Temporary password email to {Email}", email))
			.Returns(Task.CompletedTask);
			
		mock.Setup(x => x.SendEmailAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
				It.IsAny<bool>(), It.IsAny<string>(), 
				It.IsAny<List<string>>(), It.IsAny<List<string>>(), 
				It.IsAny<List<EmailAttachmentDto>>()))
			.Callback<string, string, string, bool, string, List<string>, List<string>, List<EmailAttachmentDto>>(
				(to, subj, body, html, from, cc, bcc, att) => 
					logger.LogDebug("Mock: Email to {To} with subject {Subject}", to, subj))
			.Returns(Task.CompletedTask);
			
		return mock.Object;
	}
}

/// <summary>
/// Minimal IApplicationBuilder wrapper for testing
/// </summary>
internal class ApplicationBuilderWrapper : IApplicationBuilder
{
	public ApplicationBuilderWrapper(IServiceProvider serviceProvider)
	{
		ApplicationServices = serviceProvider;
	}

	public IServiceProvider ApplicationServices { get; set; }
	public IDictionary<string, object> Properties => new Dictionary<string, object>();
	public IFeatureCollection ServerFeatures => new FeatureCollection();

	public RequestDelegate Build() => _ => Task.CompletedTask;
	public IApplicationBuilder New() => this;
	public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) => this;
}
