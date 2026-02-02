using Application.Contracts.Email;

namespace Infrastructure.Messaging.Contracts;

/// <summary>
/// MassTransit message contract for sending emails.
/// Implements the Application layer contract for Clean Architecture compliance.
/// </summary>
public record SendEmailCommand : ISendEmailCommand
{
	public Guid MessageId { get; init; }
	public string To { get; init; } = string.Empty;
	public string Subject { get; init; } = string.Empty;
	public string Body { get; init; } = string.Empty;
	public bool IsHtml { get; init; }
	public string? From { get; init; }
	public IEnumerable<string>? Cc { get; init; }
	public IEnumerable<string>? Bcc { get; init; }
	public IEnumerable<IEmailAttachment>? Attachments { get; init; }
	public DateTime RequestedAt { get; init; }
	public string? CorrelationId { get; init; }

	public SendEmailCommand()
	{
		MessageId = Guid.NewGuid();
		RequestedAt = DateTime.UtcNow;
	}

	public SendEmailCommand(
		string to,
		string subject,
		string body,
		bool isHtml = true,
		string? from = null,
		IEnumerable<string>? cc = null,
		IEnumerable<string>? bcc = null,
		IEnumerable<IEmailAttachment>? attachments = null,
		string? correlationId = null)
	{
		MessageId = Guid.NewGuid();
		To = to ?? throw new ArgumentNullException(nameof(to));
		Subject = subject ?? throw new ArgumentNullException(nameof(subject));
		Body = body ?? throw new ArgumentNullException(nameof(body));
		IsHtml = isHtml;
		From = from;
		Cc = cc;
		Bcc = bcc;
		Attachments = attachments;
		RequestedAt = DateTime.UtcNow;
		CorrelationId = correlationId;
	}
}

/// <summary>
/// Email attachment implementation for MassTransit messages
/// </summary>
public record EmailAttachment : IEmailAttachment
{
	public string FileName { get; init; } = string.Empty;
	public byte[] Content { get; init; } = Array.Empty<byte>();
	public string ContentType { get; init; } = "application/octet-stream";

	public EmailAttachment() { }

	public EmailAttachment(string fileName, byte[] content, string contentType)
	{
		FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
		Content = content ?? throw new ArgumentNullException(nameof(content));
		ContentType = contentType ?? "application/octet-stream";
	}
}
