using System;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace API.ServiceCollectionExtensions;

public static class RateLimiterExtension
{
	public static IServiceCollection AddRateLimiterFromConfiguration(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<RateLimitingSettings>(configuration.GetSection("RateLimiting"));

		services.AddRateLimiter(options =>
		{
			// Read settings at runtime via DI (supports test overrides)
			options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
			{
				var config = context.RequestServices.GetRequiredService<IConfiguration>();
				var settings = config.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();

				if (!settings.Enabled)
				{
					return RateLimitPartition.GetNoLimiter("disabled");
				}

				var partitionKey = settings.PartitionByIp
					? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
					: "global";

				return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
				{
					PermitLimit = settings.PermitLimit,
					Window = TimeSpan.FromSeconds(settings.WindowInSeconds),
					QueueLimit = settings.QueueLimit,
					QueueProcessingOrder = settings.QueueProcessingOrder
				});
			});

			options.OnRejected = async (context, cancellationToken) =>
			{
				var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
				var settings = config.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();
				context.HttpContext.Response.StatusCode = settings.RejectionStatusCode;

				if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
				{
					context.HttpContext.Response.Headers.RetryAfter =
						Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
				}

				await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
			};
		});

		return services;
	}
}

public sealed record RateLimitingSettings
{
	public bool Enabled { get; init; } = true;
	public bool PartitionByIp { get; init; } = true;
	public int PermitLimit { get; init; } = 100;
	public int WindowInSeconds { get; init; } = 60;
	public int QueueLimit { get; init; } = 0;
	public QueueProcessingOrder QueueProcessingOrder { get; init; } = QueueProcessingOrder.OldestFirst;
	public int RejectionStatusCode { get; init; } = StatusCodes.Status429TooManyRequests;
}
