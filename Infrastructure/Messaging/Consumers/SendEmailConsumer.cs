using System.Net;
using System.Net.Mail;
using Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Consumers;

/// <summary>
/// MassTransit consumer for processing email commands.
/// Sends emails directly via SMTP without depending on IEmailService to avoid circular dependencies.
/// </summary>
public class SendEmailConsumer : IConsumer<SendEmailCommand>
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<SendEmailConsumer> _logger;

	public SendEmailConsumer(
		IConfiguration configuration,
		ILogger<SendEmailConsumer> logger)
	{
		_configuration = configuration;
		_logger = logger;
	}

	public async Task Consume(ConsumeContext<SendEmailCommand> context)
	{
		var command = context.Message;

		_logger.LogInformation(
			"Processing email command {MessageId} to {Recipient} with subject: {Subject}",
			command.MessageId,
			command.To,
			command.Subject);

		try
		{
			await SendEmailViaSmtpAsync(command);

			_logger.LogInformation(
				"Successfully sent email {MessageId} to {Recipient}",
				command.MessageId,
				command.To);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex,
				"Failed to send email {MessageId} to {Recipient}",
				command.MessageId,
				command.To);

			// Throw to trigger MassTransit retry/redelivery
			throw;
		}
	}

	private async Task SendEmailViaSmtpAsync(SendEmailCommand command)
	{
		var host = _configuration["SmtpSettings:Host"];
		var port = int.TryParse(_configuration["SmtpSettings:Port"], out var p) ? p : 25;
		var user = _configuration["SmtpSettings:Username"];
		var pass = _configuration["SmtpSettings:Password"];
		var defaultFrom = _configuration["SmtpSettings:From"] ?? user ?? "noreply@example.com";
		var enableSsl = !bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out var ssl) || ssl;

		var fromName = _configuration["SmtpSettings:FromName"] ?? defaultFrom;
		var fromAddress = new MailAddress(command.From ?? defaultFrom, fromName);

		using var message = new MailMessage();
		message.From = fromAddress;
		message.To.Add(command.To);
		message.Subject = command.Subject;
		message.Body = command.Body;
		message.IsBodyHtml = command.IsHtml;

		// Add CC recipients
		if (command.Cc != null)
		{
			foreach (var ccAddress in command.Cc)
			{
				message.CC.Add(ccAddress);
			}
		}

		// Add BCC recipients
		if (command.Bcc != null)
		{
			foreach (var bccAddress in command.Bcc)
			{
				message.Bcc.Add(bccAddress);
			}
		}

		// Add attachments
		if (command.Attachments != null)
		{
			foreach (var attachment in command.Attachments)
			{
				var stream = new MemoryStream(attachment.Content);
				message.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
			}
		}

		using var client = new SmtpClient(host, port)
		{
			EnableSsl = enableSsl,
		};

		if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
		{
			client.Credentials = new NetworkCredential(user, pass);
		}

		await client.SendMailAsync(message);
	}
}
