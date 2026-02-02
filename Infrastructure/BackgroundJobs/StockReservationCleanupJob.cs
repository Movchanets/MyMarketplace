using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Background job for cleaning up expired stock reservations
/// Can be scheduled using Hangfire, Quartz, or similar job scheduler
/// </summary>
public class StockReservationCleanupJob
{
	private readonly IStockReservationCleanupService _cleanupService;
	private readonly ILogger<StockReservationCleanupJob> _logger;

	public StockReservationCleanupJob(
		IStockReservationCleanupService cleanupService,
		ILogger<StockReservationCleanupJob> logger)
	{
		_cleanupService = cleanupService;
		_logger = logger;
	}

	/// <summary>
	/// Executes the cleanup job
	/// </summary>
	/// <param name="batchSize">Maximum number of reservations to process (default: 100)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Number of reservations released</returns>
	public async Task<int> ExecuteAsync(int batchSize = 100, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Starting StockReservationCleanupJob with batch size {BatchSize}", batchSize);

		try
		{
			var releasedCount = await _cleanupService.CleanupExpiredReservationsAsync(batchSize, cancellationToken);

			if (releasedCount > 0)
			{
				_logger.LogInformation("StockReservationCleanupJob completed. Released {ReleasedCount} expired reservations", releasedCount);
			}
			else
			{
				_logger.LogDebug("StockReservationCleanupJob completed. No expired reservations found");
			}

			return releasedCount;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error executing StockReservationCleanupJob");
			throw;
		}
	}

	/// <summary>
	/// Gets current reservation statistics
	/// </summary>
	public async Task<ReservationStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
	{
		return await _cleanupService.GetReservationStatisticsAsync(cancellationToken);
	}
}

/// <summary>
/// Hosted service that schedules the cleanup job to run periodically
/// </summary>
public class StockReservationCleanupHostedService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<StockReservationCleanupHostedService> _logger;
	private readonly TimeSpan _interval;

	public StockReservationCleanupHostedService(
		IServiceProvider serviceProvider,
		ILogger<StockReservationCleanupHostedService> logger,
		TimeSpan? interval = null)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
		_interval = interval ?? TimeSpan.FromMinutes(5);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("StockReservationCleanupHostedService started with interval {Interval}", _interval);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var scope = _serviceProvider.CreateScope();
				var cleanupService = scope.ServiceProvider.GetRequiredService<IStockReservationCleanupService>();

				var releasedCount = await cleanupService.CleanupExpiredReservationsAsync(
					batchSize: 100,
					cancellationToken: stoppingToken);

				if (releasedCount > 0)
				{
					_logger.LogInformation("Scheduled cleanup released {ReleasedCount} expired reservations", releasedCount);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in scheduled stock reservation cleanup");
			}

			await Task.Delay(_interval, stoppingToken);
		}

		_logger.LogInformation("StockReservationCleanupHostedService stopped");
	}
}
