using Application.Contracts.Email;
using Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

/// <summary>
/// MassTransit-based implementation of IEmailNotificationService.
/// Publishes email commands to the message bus for asynchronous processing.
/// </summary>
public class MassTransitEmailService : IEmailNotificationService
{
	private readonly IBus _bus;
	private readonly ILogger<MassTransitEmailService> _logger;

	public MassTransitEmailService(
		IBus bus,
		ILogger<MassTransitEmailService> logger)
	{
		_bus = bus;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task SendEmailAsync(ISendEmailCommand command, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation(
			"Publishing email command {MessageId} to {Recipient}",
			command.MessageId,
			command.To);

		// Map the Application layer contract to the Infrastructure message contract
		var message = new SendEmailCommand
		{
			MessageId = command.MessageId,
			To = command.To,
			Subject = command.Subject,
			Body = command.Body,
			IsHtml = command.IsHtml,
			From = command.From,
			Cc = command.Cc,
			Bcc = command.Bcc,
			Attachments = command.Attachments?.Select(a => new EmailAttachment(
				a.FileName,
				a.Content,
				a.ContentType)),
			RequestedAt = command.RequestedAt,
			CorrelationId = command.CorrelationId
		};

		// Publish to MassTransit (uses PostgreSQL transport with outbox)
		await _bus.Publish(message, cancellationToken);

		_logger.LogInformation(
			"Successfully published email command {MessageId}",
			command.MessageId);
	}

	/// <inheritdoc />
	public async Task SendTemplatedEmailAsync(
		string templateName,
		string to,
		object templateData,
		CancellationToken cancellationToken = default)
	{
		_logger.LogInformation(
			"Sending templated email using template {TemplateName} to {Recipient}",
			templateName,
			to);

		// Render template (simplified - in production, use a template engine)
		var (subject, body) = RenderTemplate(templateName, templateData);

		var command = new SendEmailCommand(
			to: to,
			subject: subject,
			body: body,
			isHtml: true);

		await SendEmailAsync(command, cancellationToken);
	}

	/// <inheritdoc />
	public async Task ScheduleEmailAsync(
		ISendEmailCommand command,
		DateTime scheduledTime,
		CancellationToken cancellationToken = default)
	{
		_logger.LogInformation(
			"Scheduling email {MessageId} for {ScheduledTime}",
			command.MessageId,
			scheduledTime);

		var message = new SendEmailCommand
		{
			MessageId = command.MessageId,
			To = command.To,
			Subject = command.Subject,
			Body = command.Body,
			IsHtml = command.IsHtml,
			From = command.From,
			Cc = command.Cc,
			Bcc = command.Bcc,
			Attachments = command.Attachments?.Select(a => new EmailAttachment(
				a.FileName,
				a.Content,
				a.ContentType)),
			RequestedAt = command.RequestedAt,
			CorrelationId = command.CorrelationId
		};

		// Schedule message using MassTransit - convert DateTime to TimeSpan delay
		var delay = scheduledTime > DateTime.UtcNow
			? scheduledTime - DateTime.UtcNow
			: TimeSpan.Zero;
		await _bus.Publish(message, ctx => ctx.Delay = delay, cancellationToken);

		_logger.LogInformation(
			"Successfully scheduled email {MessageId} for {ScheduledTime}",
			command.MessageId,
			scheduledTime);
	}

	/// <summary>
	/// Renders an email template with the provided data
	/// </summary>
	private (string Subject, string Body) RenderTemplate(string templateName, object data)
	{
		// Simplified template rendering - in production, use a proper template engine
		// like RazorLight, Handlebars.NET, or Scriban
		return templateName switch
		{
			EmailTemplates.OrderConfirmation => ("Order Confirmation", "<h1>Thank you for your order!</h1>"),
			EmailTemplates.OrderShipped => ("Your Order Has Shipped", "<h1>Your order is on its way!</h1>"),
			EmailTemplates.OrderDelivered => ("Order Delivered", "<h1>Your order has been delivered!</h1>"),
			EmailTemplates.OrderCancelled => ("Order Cancelled", "<h1>Your order has been cancelled</h1>"),
			EmailTemplates.PaymentReceived => ("Payment Received", "<h1>Thank you for your payment</h1>"),
			EmailTemplates.PaymentFailed => ("Payment Failed", "<h1>There was an issue with your payment</h1>"),
			EmailTemplates.Welcome => ("Welcome!", "<h1>Welcome to our store!</h1>"),
			EmailTemplates.PasswordReset => ("Password Reset", "<h1>Reset your password</h1>"),
			EmailTemplates.CartReminder => ("Don't forget your cart!", "<h1>You have items in your cart</h1>"),
			EmailTemplates.CartAbandoned => ("Still thinking it over?", "<h1>Your cart is waiting</h1>"),
			_ => ("Notification", "<h1>You have a new notification</h1>")
		};
	}
}
