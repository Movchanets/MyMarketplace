using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace API.ServiceCollectionExtensions;

/// <summary>
/// Extension methods for configuring Redis caching and Output Cache
/// </summary>
public static class RedisCacheExtension
{
	/// <summary>
	/// Adds Redis distributed cache and Output Cache with predefined policies.
	/// Falls back to in-memory cache if Redis is disabled.
	/// </summary>
	public static IHostApplicationBuilder AddRedisCache(
		this IHostApplicationBuilder builder)
	{
		var configuration = builder.Configuration;
		var redisConfig = configuration.GetSection("Redis");
		var enabled = redisConfig.GetValue<bool>("Enabled");
		var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<IHostApplicationBuilder>>();

		if (!enabled)
		{
			// Fallback to in-memory distributed cache
			builder.Services.AddDistributedMemoryCache();
			AddOutputCachePolicies(builder.Services, useRedis: false);
			// Register cache invalidation service
			builder.Services.AddScoped<ICacheInvalidationService, OutputCacheInvalidationService>();
			// Basic health check (always healthy when Redis disabled)
			builder.Services.AddHealthChecks();
			Console.WriteLine("[CACHE] Redis cache is DISABLED. Using in-memory cache.");
			return builder;
		}

		// Check for Aspire connection string first (passed via ConnectionStrings__redis env var)
		var aspireConnectionString = configuration.GetConnectionString("redis");
		var configConnectionString = redisConfig["ConnectionString"] ?? "localhost:6379";
		var instanceName = redisConfig["InstanceName"] ?? "AppNet9:";

		Console.WriteLine("[CACHE] Attempting to connect to Redis: {0}", configConnectionString);

		if (!string.IsNullOrEmpty(aspireConnectionString))
		{
			// Use Aspire Redis client integration (handles service discovery automatically)
			Console.WriteLine("[CACHE] Using Aspire Redis client integration");
			builder.AddRedisDistributedCache("redis", settings =>
			{
				settings.DisableTracing = false;
			});

			builder.AddRedisOutputCache("redis", settings =>
			{
				settings.DisableTracing = false;
			});
		}
		else
		{
			// Fallback: Use standard Redis configuration from appsettings
			Console.WriteLine("[CACHE] Using standard StackExchange Redis configuration");
			builder.Services.AddStackExchangeRedisCache(options =>
			{
				options.Configuration = configConnectionString;
				options.InstanceName = instanceName;
			});

			// Output caching with Redis backend
			builder.Services.AddStackExchangeRedisOutputCache(options =>
			{
				options.Configuration = configConnectionString;
				options.InstanceName = instanceName + "OutputCache:";
			});
		}

		// Add output cache policies
		AddOutputCachePolicies(builder.Services);

		// Register cache invalidation service
		builder.Services.AddScoped<ICacheInvalidationService, OutputCacheInvalidationService>();

		// Health check for Redis
		var healthCheckConnectionString = aspireConnectionString ?? configConnectionString;
		builder.Services.AddHealthChecks()
			.AddRedis(healthCheckConnectionString, name: "redis", tags: ["cache", "redis"]);

		Console.WriteLine("[CACHE] Redis cache configured successfully. Instance: {0}", instanceName);
		return builder;
	}

	private static void AddOutputCachePolicies(IServiceCollection services, bool useRedis = true)
	{
		services.AddOutputCache(options =>
		{
			// Default policy - no caching
			options.AddBasePolicy(builder => builder.NoCache());

			// Categories - довідник, рідко змінюється
			options.AddPolicy("Categories", builder => builder
				.Expire(TimeSpan.FromMinutes(10))
				.SetVaryByQuery("parentCategoryId", "topLevelOnly")
				.Tag("categories"));

			// Tags - довідник
			options.AddPolicy("Tags", builder => builder
				.Expire(TimeSpan.FromMinutes(10))
				.Tag("tags"));

			// Attribute Definitions - довідник атрибутів
			options.AddPolicy("AttributeDefinitions", builder => builder
				.Expire(TimeSpan.FromMinutes(15))
				.SetVaryByQuery("includeInactive")
				.Tag("attribute-definitions"));

			// Products list - публічний каталог
			options.AddPolicy("Products", builder => builder
				.Expire(TimeSpan.FromMinutes(2))
				.Tag("products"));

			// Product details by id/slug/sku
			options.AddPolicy("ProductDetails", builder => builder
				.Expire(TimeSpan.FromMinutes(2))
				.SetVaryByRouteValue("id", "productSlug", "skuCode")
				.SetVaryByQuery("sku")
				.Tag("products"));

			// Products by category
			options.AddPolicy("ProductsByCategory", builder => builder
				.Expire(TimeSpan.FromMinutes(2))
				.SetVaryByRouteValue("categoryId")
				.Tag("products", "categories"));

			// Stores - публічні сторінки магазинів
			options.AddPolicy("Stores", builder => builder
				.Expire(TimeSpan.FromMinutes(5))
				.SetVaryByRouteValue("slug")
				.Tag("stores"));
		});
	}
}
