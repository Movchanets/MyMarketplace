namespace Application.Interfaces;

/// <summary>
/// Service for acquiring and releasing distributed locks to prevent concurrent operations
/// </summary>
public interface IDistributedLockService
{
	/// <summary>
	/// Attempts to acquire a distributed lock
	/// </summary>
	/// <param name="lockKey">Unique identifier for the lock</param>
	/// <param name="lockValue">Unique value to identify the lock holder</param>
	/// <param name="ttl">Time-to-live for the lock (auto-release after this time)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if lock was acquired, false if already locked</returns>
	Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan ttl, CancellationToken cancellationToken = default);

	/// <summary>
	/// Releases a distributed lock
	/// </summary>
	/// <param name="lockKey">Unique identifier for the lock</param>
	/// <param name="lockValue">Unique value to verify the lock holder</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if lock was released, false if not found or different holder</returns>
	Task<bool> ReleaseLockAsync(string lockKey, string lockValue, CancellationToken cancellationToken = default);

	/// <summary>
	/// Extends the TTL of an existing lock
	/// </summary>
	/// <param name="lockKey">Unique identifier for the lock</param>
	/// <param name="lockValue">Unique value to verify the lock holder</param>
	/// <param name="additionalTtl">Additional time to add to the lock</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if lock was extended, false if not found or different holder</returns>
	Task<bool> ExtendLockAsync(string lockKey, string lockValue, TimeSpan additionalTtl, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a lock exists and is held by the specified value
	/// </summary>
	/// <param name="lockKey">Unique identifier for the lock</param>
	/// <param name="lockValue">Unique value to verify</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if lock exists and is held by the specified value</returns>
	Task<bool> IsLockHeldByAsync(string lockKey, string lockValue, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes an action within a distributed lock, automatically releasing on completion
	/// </summary>
	/// <typeparam name="T">Return type of the action</typeparam>
	/// <param name="lockKey">Unique identifier for the lock</param>
	/// <param name="lockValue">Unique value to identify the lock holder</param>
	/// <param name="ttl">Time-to-live for the lock</param>
	/// <param name="action">Action to execute within the lock</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result of the action, or default if lock could not be acquired</returns>
	Task<T?> ExecuteWithLockAsync<T>(string lockKey, string lockValue, TimeSpan ttl, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes an action within a distributed lock, automatically releasing on completion
	/// </summary>
	/// <param name="lockKey">Unique identifier for the lock</param>
	/// <param name="lockValue">Unique value to identify the lock holder</param>
	/// <param name="ttl">Time-to-live for the lock</param>
	/// <param name="action">Action to execute within the lock</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if action was executed, false if lock could not be acquired</returns>
	Task<bool> ExecuteWithLockAsync(string lockKey, string lockValue, TimeSpan ttl, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods for distributed lock service
/// </summary>
public static class DistributedLockServiceExtensions
{
	/// <summary>
	/// Generates a lock key for preventing concurrent checkout for a user
	/// </summary>
	public static string GenerateCheckoutLockKey(this IDistributedLockService _, Guid userId)
	{
		return $"checkout:user:{userId}";
	}

	/// <summary>
	/// Generates a lock key for preventing concurrent checkout for a cart
	/// </summary>
	public static string GenerateCartLockKey(this IDistributedLockService _, Guid cartId)
	{
		return $"checkout:cart:{cartId}";
	}

	/// <summary>
	/// Generates a lock key for preventing concurrent order processing
	/// </summary>
	public static string GenerateOrderProcessingLockKey(this IDistributedLockService _, Guid orderId)
	{
		return $"order:processing:{orderId}";
	}

	/// <summary>
	/// Generates a lock value (unique identifier for this lock holder)
	/// </summary>
	public static string GenerateLockValue(this IDistributedLockService _)
	{
		return Guid.NewGuid().ToString("N");
	}
}
