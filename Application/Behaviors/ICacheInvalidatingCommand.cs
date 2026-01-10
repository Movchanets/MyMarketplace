using MediatR;

namespace Application.Behaviors;

/// <summary>
/// Marker interface for commands that should invalidate cache after successful execution.
/// Implement this interface on your commands to automatically evict cached data by tags.
/// </summary>
public interface ICacheInvalidatingCommand
{
	/// <summary>
	/// Cache tags to invalidate after the command completes successfully.
	/// Common tags: "categories", "tags", "products", "stores", "attribute-definitions"
	/// </summary>
	IEnumerable<string> CacheTags { get; }
}
