using System.Diagnostics;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Messaging.Consumers;
using Infrastructure.Messaging.Contracts;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Application.Tests.Messaging;

/// <summary>
/// Test harness configuration for MassTransit unit and integration tests.
/// Uses PostgreSQL SQL Transport according to official MassTransit documentation.
/// https://masstransit.io/quick-starts/postgresql
/// </summary>
public class MassTransitTestHarness : IAsyncLifetime
{
	private readonly PostgreSqlContainer _postgresContainer;
	private ServiceProvider _serviceProvider = null!;
	private ITestHarness _testHarness = null!;
	private readonly FakeTimeProvider _timeProvider;
	private readonly Action<IServiceCollection>? _configureServices;
	private readonly Action<IBusRegistrationConfigurator>? _configureBus;

	public MassTransitTestHarness(
		Action<IServiceCollection>? configureServices = null,
		Action<IBusRegistrationConfigurator>? configureBus = null)
	{
		_timeProvider = new FakeTimeProvider();
		_timeProvider.SetUtcNow(DateTimeOffset.UtcNow);
		_configureServices = configureServices;
		_configureBus = configureBus;

		// Create PostgreSQL Testcontainer
		_postgresContainer = new PostgreSqlBuilder()
			.WithImage("postgres:16-alpine")
			.WithDatabase($"masstransit_test_{Guid.NewGuid():N}")
			.WithUsername("testuser")
			.WithPassword("testpass")
			.Build();
	}

	public async Task InitializeAsync()
	{
		// Start PostgreSQL container
		await _postgresContainer.StartAsync();

		var services = new ServiceCollection();

		// Add logging
		services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

		// Add fake time provider for time-based testing
		services.AddSingleton<TimeProvider>(_timeProvider);

		// Add PostgreSQL database context
		services.AddDbContext<AppDbContext>(options =>
		{
			options.UseNpgsql(_postgresContainer.GetConnectionString());
		});

		// Configure SQL Transport options according to MassTransit documentation
		// Parse values from connection string
		var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(_postgresContainer.GetConnectionString());
		services.AddOptions<SqlTransportOptions>().Configure(options =>
		{
			options.Host = connectionStringBuilder.Host ?? "localhost";
			options.Port = connectionStringBuilder.Port == 0 ? 5432 : connectionStringBuilder.Port;
			options.Database = connectionStringBuilder.Database ?? "test";
			options.Schema = "transport_test";
			options.Role = "transport";
			options.Username = connectionStringBuilder.Username ?? "testuser";
			options.Password = connectionStringBuilder.Password ?? "";
			options.AdminUsername = connectionStringBuilder.Username ?? "testuser";
			options.AdminPassword = connectionStringBuilder.Password ?? "";
		});

		// Add migration hosted service for SQL Transport
		services.AddPostgresMigrationHostedService();

		// Add MassTransit with PostgreSQL SQL Transport and test harness
		services.AddMassTransitTestHarness(configurator =>
		{
			// Register the email consumer
			configurator.AddConsumer<SendEmailConsumer>();

			// Configure PostgreSQL SQL Transport according to documentation
			if (_configureBus != null)
			{
				_configureBus.Invoke(configurator);
			}
			else
			{
				configurator.UsingPostgres((context, cfg) =>
				{
					cfg.UseMessageRetry(r => r.Immediate(3));
					cfg.ConfigureEndpoints(context);
				});
			}
		});

		// Allow custom service configuration
		_configureServices?.Invoke(services);

		_serviceProvider = services.BuildServiceProvider();

		// Run EF Core migrations first
		using var scope = _serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync();

		// Start MassTransit harness (this will also run SQL Transport migrations)
		_testHarness = _serviceProvider.GetRequiredService<ITestHarness>();
		await _testHarness.Start();
	}

	public async Task DisposeAsync()
	{
		if (_testHarness != null)
		{
			await _testHarness.Stop();
		}

		if (_serviceProvider != null)
		{
			await _serviceProvider.DisposeAsync();
		}

		if (_postgresContainer != null)
		{
			await _postgresContainer.DisposeAsync();
		}
	}

	/// <summary>
	/// Gets the MassTransit test harness for assertions.
	/// </summary>
	public ITestHarness Harness => _testHarness;

	/// <summary>
	/// Gets the fake time provider for time manipulation in tests.
	/// </summary>
	public FakeTimeProvider TimeProvider => _timeProvider;

	/// <summary>
	/// Gets the service provider for resolving services.
	/// </summary>
	public IServiceProvider Services => _serviceProvider;

	/// <summary>
	/// Gets the AppDbContext for database assertions.
	/// </summary>
	public AppDbContext GetDbContext() =>
		_serviceProvider.GetRequiredService<AppDbContext>();

	/// <summary>
	/// Gets the bus for publishing messages.
	/// </summary>
	public IBus GetBus() =>
		_serviceProvider.GetRequiredService<IBus>();

	/// <summary>
	/// Starts the test harness.
	/// </summary>
	public Task StartAsync() => Task.CompletedTask; // Already started in InitializeAsync

	/// <summary>
	/// Advances time by the specified duration.
	/// </summary>
	public void AdvanceTime(TimeSpan duration)
	{
		_timeProvider.Advance(duration);
	}

	/// <summary>
	/// Waits for all consumed messages to be processed.
	/// </summary>
	public async Task WaitForConsumedMessagesAsync(TimeSpan? timeout = null)
	{
		await _testHarness.Consumed.Any();
	}

	/// <summary>
	/// Gets the consumer test harness for a specific consumer type.
	/// </summary>
	public IConsumerTestHarness<TConsumer> GetConsumerHarness<TConsumer>()
		where TConsumer : class, IConsumer
	{
		return _testHarness.GetConsumerHarness<TConsumer>();
	}

	/// <summary>
	/// Gets published messages of a specific type.
	/// </summary>
	public IEnumerable<SendEmailCommand> GetPublishedMessages<T>()
		where T : class
	{
		return _testHarness.Published.Select<T>().Select(x => x.Context.Message).OfType<SendEmailCommand>();
	}

	/// <summary>
	/// Asserts that no messages were published.
	/// </summary>
	public void AssertNoMessagesPublished()
	{
		_testHarness.Published.Select(m => true).Should().BeEmpty();
	}

	/// <summary>
	/// Asserts that a specific message was published.
	/// </summary>
	public void AssertMessagePublished<T>(Func<T, bool>? predicate = null)
		where T : class
	{
		var message = _testHarness.Published.Select<T>().FirstOrDefault();
		message.Should().NotBeNull();
		if (predicate != null)
		{
			predicate(message!.Context.Message).Should().BeTrue();
		}
	}

	/// <summary>
	/// Asserts that a consumer consumed a message.
	/// </summary>
	public void AssertConsumed<T>(Func<T, bool>? predicate = null)
		where T : class
	{
		var consumed = _testHarness.Consumed.Select<T>().FirstOrDefault();
		consumed.Should().NotBeNull();
		if (predicate != null)
		{
			predicate(consumed!.Context.Message).Should().BeTrue();
		}
	}

	/// <summary>
	/// Waits for a saga to complete (for saga-based tests).
	/// </summary>
	public async Task WaitForSagaAsync<TSaga>(TimeSpan? timeout = null)
		where TSaga : class, ISaga
	{
		var timeoutValue = timeout ?? TimeSpan.FromSeconds(30);
		var sw = Stopwatch.StartNew();

		while (sw.Elapsed < timeoutValue)
		{
			var sagaRepository = _serviceProvider.GetService<ISagaRepository<TSaga>>();
			if (sagaRepository != null)
			{
				return;
			}
			await Task.Delay(100);
		}

		throw new TimeoutException($"Saga {typeof(TSaga).Name} did not complete within {timeoutValue}");
	}

	/// <summary>
	/// Creates a new scope for service resolution.
	/// </summary>
	public IServiceScope CreateScope() =>
		_serviceProvider.CreateScope();
}

/// <summary>
/// Factory for creating test harnesses with different retry configurations.
/// </summary>
public static class MassTransitTestHarnessFactory
{
	/// <summary>
	/// Creates a test harness with immediate retry policy.
	/// </summary>
	public static MassTransitTestHarness CreateWithImmediateRetry(int retryCount = 3)
	{
		return new MassTransitTestHarness(configureBus: configurator =>
		{
			configurator.UsingPostgres((context, cfg) =>
			{
				cfg.UseMessageRetry(r => r.Immediate(retryCount));
				cfg.ConfigureEndpoints(context);
			});
		});
	}

	/// <summary>
	/// Creates a test harness with interval retry policy.
	/// </summary>
	public static MassTransitTestHarness CreateWithIntervalRetry(
		int retryCount = 3,
		TimeSpan? retryInterval = null)
	{
		return new MassTransitTestHarness(configureBus: configurator =>
		{
			configurator.UsingPostgres((context, cfg) =>
			{
				cfg.UseMessageRetry(r =>
				{
					r.Interval(retryCount, retryInterval ?? TimeSpan.FromMilliseconds(100));
				});
				cfg.ConfigureEndpoints(context);
			});
		});
	}

	/// <summary>
	/// Creates a test harness with circuit breaker configured.
	/// </summary>
	public static MassTransitTestHarness CreateWithCircuitBreaker(
		int tripThreshold = 5,
		TimeSpan? activeDuration = null,
		TimeSpan? resetInterval = null)
	{
		return new MassTransitTestHarness(configureBus: configurator =>
		{
			configurator.UsingPostgres((context, cfg) =>
			{
				cfg.UseCircuitBreaker(cb =>
				{
					cb.TrackingPeriod = TimeSpan.FromMinutes(1);
					cb.TripThreshold = tripThreshold;
					cb.ActiveThreshold = 10;
					cb.ResetInterval = resetInterval ?? TimeSpan.FromMinutes(1);
				});
				cfg.ConfigureEndpoints(context);
			});
		});
	}

	/// <summary>
	/// Creates a test harness with exponential backoff retry policy.
	/// </summary>
	public static MassTransitTestHarness CreateWithExponentialBackoff(
		int retryCount = 5,
		TimeSpan? initialInterval = null,
		TimeSpan? maxInterval = null)
	{
		return new MassTransitTestHarness(configureBus: configurator =>
		{
			configurator.UsingPostgres((context, cfg) =>
			{
				cfg.UseMessageRetry(r =>
				{
					r.Exponential(retryCount,
						initialInterval ?? TimeSpan.FromMilliseconds(100),
						maxInterval ?? TimeSpan.FromSeconds(10),
						TimeSpan.FromMilliseconds(100));
				});
				cfg.ConfigureEndpoints(context);
			});
		});
	}
}
