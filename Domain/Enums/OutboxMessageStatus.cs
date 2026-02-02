namespace Domain.Enums;

/// <summary>
/// Represents the status of an outbox message
/// </summary>
public enum OutboxMessageStatus
{
	/// <summary>
	/// Message is pending publication
	/// </summary>
	Pending = 0,

	/// <summary>
	/// Message has been successfully published
	/// </summary>
	Published = 1,

	/// <summary>
	/// Message publication failed but will be retried
	/// </summary>
	Failed = 2,

	/// <summary>
	/// Message has exceeded retry limit and is permanently failed
	/// </summary>
	DeadLetter = 3
}

/// <summary>
/// Extension methods for OutboxMessageStatus enum
/// </summary>
public static class OutboxMessageStatusExtensions
{
	/// <summary>
	/// Checks if the message can be retried
	/// </summary>
	public static bool CanRetry(this OutboxMessageStatus status)
	{
		return status == OutboxMessageStatus.Pending || status == OutboxMessageStatus.Failed;
	}

	/// <summary>
	/// Checks if the message is in a final state
	/// </summary>
	public static bool IsFinalState(this OutboxMessageStatus status)
	{
		return status == OutboxMessageStatus.Published || status == OutboxMessageStatus.DeadLetter;
	}
}
