namespace Application.Interfaces;

/// <summary>
/// Service for invalidating cache entries by tags
/// </summary>
public interface ICacheInvalidationService
{
	/// <summary>
	/// Invalidates all cache entries with the specified tag
	/// </summary>
	Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);

	/// <summary>
	/// Invalidates all cache entries with any of the specified tags
	/// </summary>
	Task InvalidateByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}
