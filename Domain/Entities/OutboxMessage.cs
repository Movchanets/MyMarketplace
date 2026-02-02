using System.Text.Json;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Outbox message entity for transactional outbox pattern.
/// Ensures domain events are persisted atomically with business data changes.
/// </summary>
public class OutboxMessage : BaseEntity<Guid>
{
	/// <summary>
	/// Type of the event (fully qualified type name)
	/// </summary>
	public string EventType { get; private set; } = string.Empty;

	/// <summary>
	/// Serialized event payload (JSON)
	/// </summary>
	public string Payload { get; private set; } = string.Empty;

	/// <summary>
	/// Correlation ID for tracking related operations
	/// </summary>
	public string? CorrelationId { get; private set; }

	/// <summary>
	/// Aggregate ID that the event relates to
	/// </summary>
	public string? AggregateId { get; private set; }

	/// <summary>
	/// Type of aggregate (e.g., "Order", "Cart")
	/// </summary>
	public string? AggregateType { get; private set; }

	/// <summary>
	/// Current status of the message
	/// </summary>
	public OutboxMessageStatus Status { get; private set; }

	/// <summary>
	/// Number of retry attempts
	/// </summary>
	public int RetryCount { get; private set; }

	/// <summary>
	/// When the message was created
	/// </summary>
	public DateTime CreatedAt { get; private set; }

	/// <summary>
	/// When the message was last processed
	/// </summary>
	public DateTime? ProcessedAt { get; private set; }

	/// <summary>
	/// Error message from last failure
	/// </summary>
	public string? ErrorMessage { get; private set; }

	/// <summary>
	/// Scheduled time for next retry (null if not scheduled)
	/// </summary>
	public DateTime? ScheduledFor { get; private set; }

	/// <summary>
	/// Maximum number of retry attempts before moving to dead letter
	/// </summary>
	public const int MaxRetryCount = 5;

	private OutboxMessage() { }

	/// <summary>
	/// Creates a new outbox message
	/// </summary>
	public OutboxMessage(
		string eventType,
		string payload,
		string? correlationId = null,
		string? aggregateId = null,
		string? aggregateType = null)
	{
		if (string.IsNullOrWhiteSpace(eventType))
			throw new ArgumentException("Event type is required", nameof(eventType));

		if (string.IsNullOrWhiteSpace(payload))
			throw new ArgumentException("Payload is required", nameof(payload));

		Id = Guid.NewGuid();
		EventType = eventType.Trim();
		Payload = payload;
		CorrelationId = correlationId;
		AggregateId = aggregateId;
		AggregateType = aggregateType;
		Status = OutboxMessageStatus.Pending;
		RetryCount = 0;
		CreatedAt = DateTime.UtcNow;
	}

	/// <summary>
	/// Creates an outbox message from a domain event
	/// </summary>
	public static OutboxMessage FromDomainEvent<TEvent>(
		TEvent domainEvent,
		string? correlationId = null,
		string? aggregateId = null,
		string? aggregateType = null) where TEvent : class
	{
		var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
		var payload = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});

		return new OutboxMessage(eventType, payload, correlationId, aggregateId, aggregateType);
	}

	/// <summary>
	/// Marks the message as successfully published
	/// </summary>
	public void MarkAsPublished()
	{
		Status = OutboxMessageStatus.Published;
		ProcessedAt = DateTime.UtcNow;
		ErrorMessage = null;
		ScheduledFor = null;
		MarkAsUpdated();
	}

	/// <summary>
	/// Marks the message as failed and schedules for retry
	/// </summary>
	public void MarkAsFailed(string errorMessage)
	{
		RetryCount++;
		ErrorMessage = errorMessage;

		if (RetryCount >= MaxRetryCount)
		{
			Status = OutboxMessageStatus.DeadLetter;
			ScheduledFor = null;
		}
		else
		{
			Status = OutboxMessageStatus.Failed;
			// Exponential backoff: 1min, 2min, 4min, 8min, 16min
			var delayMinutes = Math.Pow(2, RetryCount - 1);
			ScheduledFor = DateTime.UtcNow.AddMinutes(delayMinutes);
		}

		ProcessedAt = DateTime.UtcNow;
		MarkAsUpdated();
	}

	/// <summary>
	/// Resets the message for retry (e.g., manual retry from admin)
	/// </summary>
	public void ResetForRetry()
	{
		if (Status == OutboxMessageStatus.DeadLetter)
		{
			RetryCount = 0;
		}

		Status = OutboxMessageStatus.Pending;
		ErrorMessage = null;
		ScheduledFor = null;
		MarkAsUpdated();
	}

	/// <summary>
	/// Checks if the message is ready to be processed
	/// </summary>
	public bool IsReadyForProcessing()
	{
		if (Status.IsFinalState())
			return false;

		if (ScheduledFor.HasValue && ScheduledFor.Value > DateTime.UtcNow)
			return false;

		return true;
	}

	/// <summary>
	/// Deserializes the payload to the specified event type
	/// </summary>
	public TEvent? DeserializePayload<TEvent>() where TEvent : class
	{
		try
		{
			return JsonSerializer.Deserialize<TEvent>(Payload, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});
		}
		catch
		{
			return null;
		}
	}
}
