namespace Application.Contracts.Email;

/// <summary>
/// Command contract for sending email notifications.
/// This interface is defined in the Application layer to maintain Clean Architecture boundaries.
/// Implementations reside in the Infrastructure layer.
/// </summary>
public interface ISendEmailCommand
{
	/// <summary>
	/// Unique identifier for this email request (for idempotency)
	/// </summary>
	Guid MessageId { get; }

	/// <summary>
	/// Recipient email address
	/// </summary>
	string To { get; }

	/// <summary>
	/// Email subject
	/// </summary>
	string Subject { get; }

	/// <summary>
	/// Email body (HTML or plain text)
	/// </summary>
	string Body { get; }

	/// <summary>
	/// Whether the body is HTML
	/// </summary>
	bool IsHtml { get; }

	/// <summary>
	/// Optional sender email address (uses default if not specified)
	/// </summary>
	string? From { get; }

	/// <summary>
	/// Optional CC recipients
	/// </summary>
	IEnumerable<string>? Cc { get; }

	/// <summary>
	/// Optional BCC recipients
	/// </summary>
	IEnumerable<string>? Bcc { get; }

	/// <summary>
	/// Optional attachments
	/// </summary>
	IEnumerable<IEmailAttachment>? Attachments { get; }

	/// <summary>
	/// When the email was requested
	/// </summary>
	DateTime RequestedAt { get; }

	/// <summary>
	/// Correlation ID for tracking across services
	/// </summary>
	string? CorrelationId { get; }
}

/// <summary>
/// Email attachment contract
/// </summary>
public interface IEmailAttachment
{
	string FileName { get; }
	byte[] Content { get; }
	string ContentType { get; }
}
