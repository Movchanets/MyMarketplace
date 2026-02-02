using Infrastructure.Messaging.Consumers;
using Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Messaging;

/// <summary>
/// Extension methods for configuring MassTransit with PostgreSQL SQL Transport
/// Following official MassTransit documentation: https://masstransit.io/quick-starts/postgresql
/// </summary>
public static class MassTransitConfiguration
{
	/// <summary>
	/// Adds MassTransit with PostgreSQL SQL Transport to the service collection
	/// Uses PostgreSQL as the message broker (no external message broker needed)
	/// </summary>
	public static IServiceCollection AddMassTransitWithPostgresSqlTransport(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Use NeonConnection in production (same as EF Core), DefaultConnection in development
		var connectionString = configuration.GetConnectionString("NeonConnection")
			?? configuration.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException("NeonConnection or DefaultConnection connection string is required");

		// Configure SQL Transport options according to documentation
		services.AddOptions<SqlTransportOptions>().Configure(options =>
		{
			// Parse connection string to extract host and database
			var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);

			options.Host = builder.Host ?? "localhost";
			options.Port = builder.Port == 0 ? 5432 : builder.Port;
			options.Database = builder.Database ?? "postgres";
			options.Schema = "masstransit_transport";  // Dedicated schema for transport tables
			options.Role = "masstransit";              // Role for transport operations
			options.Username = builder.Username ?? "postgres";
			options.Password = builder.Password ?? "";

			// Admin credentials for migrations (can be same as regular user for development)
			options.AdminUsername = builder.Username ?? "postgres";
			options.AdminPassword = builder.Password ?? "";
		});

		// Add migration hosted service (runs migrations on startup)
		services.AddPostgresMigrationHostedService();

		services.AddMassTransit(x =>
		{
			// Add consumers
			x.AddConsumer<SendEmailConsumer>();

			// Configure PostgreSQL SQL Transport
			// MassTransit 8: SQL Transport is FREE (no license required)
			// MassTransit 9+: Requires commercial license
			x.UsingPostgres((context, cfg) =>
			{
				// Configure receive endpoint for email commands
				cfg.ReceiveEndpoint("send-email-queue", e =>
				{
					e.ConfigureConsumer<SendEmailConsumer>(context);

					// Retry configuration
					e.UseMessageRetry(r =>
					{
						r.Interval(3, TimeSpan.FromSeconds(5));
						r.Handle<Exception>();
					});

					// Dead letter configuration with delayed redelivery
					e.UseDelayedRedelivery(r =>
					{
						r.Intervals(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15));
					});
				});

				cfg.ConfigureEndpoints(context);
			});

			// Optional: Add Entity Framework outbox for atomic message delivery
			// This ensures messages are only published after database transaction commits
			x.AddEntityFrameworkOutbox<AppDbContext>(o =>
			{
				o.QueryDelay = TimeSpan.FromSeconds(10);
				o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
				o.UsePostgres();
				o.UseBusOutbox();
			});
		});

		return services;
	}

	/// <summary>
	/// Adds MassTransit with PostgreSQL SQL Transport for testing
	/// Uses provided connection string directly without reading from configuration
	/// </summary>
	public static IServiceCollection AddMassTransitWithPostgresSqlTransportForTesting(
		this IServiceCollection services,
		string connectionString)
	{
		// Configure SQL Transport options for testing
		services.AddOptions<SqlTransportOptions>().Configure(options =>
		{
			var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);

			options.Host = builder.Host ?? "localhost";
			options.Port = builder.Port == 0 ? 5432 : builder.Port;
			options.Database = builder.Database ?? "test";
			options.Schema = "transport_test";
			options.Role = "transport";
			options.Username = builder.Username ?? "test";
			options.Password = builder.Password ?? "";
			options.AdminUsername = builder.Username ?? "test";
			options.AdminPassword = builder.Password ?? "";
		});

		services.AddPostgresMigrationHostedService();

		services.AddMassTransit(x =>
		{
			x.AddConsumer<SendEmailConsumer>();

			x.UsingPostgres((context, cfg) =>
			{
				cfg.ReceiveEndpoint("send-email-queue-test", e =>
				{
					e.ConfigureConsumer<SendEmailConsumer>(context);
					e.UseMessageRetry(r => r.Immediate(3));
				});

				cfg.ConfigureEndpoints(context);
			});
		});

		return services;
	}

	/// <summary>
	/// Adds MassTransit hosted service for background message processing
	/// </summary>
	public static IServiceCollection AddMassTransitBusHostedService(
		this IServiceCollection services)
	{
		services.AddMassTransitHostedService();
		return services;
	}
}
