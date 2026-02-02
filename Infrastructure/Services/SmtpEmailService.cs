using System.IO;
using System.Net;
using System.Net.Mail;
using Application.Contracts.Email;
using Application.Interfaces;
using Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Application.Interfaces.IEmailService;

namespace Infrastructure.Services;

/// <summary>
/// Email service that publishes messages to MassTransit SQL Transport.
/// Messages are processed by SendEmailConsumer which sends emails via SMTP.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IBus _bus;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IBus bus, IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _bus = bus;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string callbackUrl)
    {
        // Try to load HTML template if configured, otherwise use plaintext
        var templatePath = _configuration["SmtpSettings:TemplatePath"];
        string body;
        bool isHtml = false;
        if (!string.IsNullOrEmpty(templatePath) && System.IO.File.Exists(templatePath))
        {
            body = await System.IO.File.ReadAllTextAsync(templatePath);
            body = body.Replace("{{CallbackUrl}}", callbackUrl);
            body = body.Replace("{{Email}}", toEmail);
            isHtml = true;
        }
        else
        {
            body = $"Please use the following link to reset your password:\n\n{callbackUrl}\n\nIf you didn't request this, ignore this email.";
        }

        var from = _configuration["SmtpSettings:From"] ?? _configuration["SmtpSettings:Username"] ?? "noreply@example.com";

        // Publish to MassTransit SQL Transport for reliable delivery
        var command = new SendEmailCommand
        {
            To = toEmail,
            Subject = "Password reset",
            Body = body,
            IsHtml = isHtml,
            From = from,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _logger.LogInformation("Publishing password reset email to {Email} via MassTransit SQL Transport", toEmail);
        await _bus.Publish(command);
        _logger.LogInformation("Password reset email published to {Email}", toEmail);
    }

    public async Task SendTemporaryPasswordEmailAsync(string toEmail, string temporaryPassword)
    {
        var from = _configuration["SmtpSettings:From"] ?? _configuration["SmtpSettings:Username"] ?? "noreply@example.com";
        var body = $"Ваш тимчасовий пароль для входу: {temporaryPassword}\n\nБудь ласка, змініть його після першого входу.";

        // Publish to MassTransit SQL Transport for reliable delivery
        var command = new SendEmailCommand
        {
            To = toEmail,
            Subject = "Тимчасовий пароль",
            Body = body,
            IsHtml = false,
            From = from,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _logger.LogInformation("Publishing temporary password email to {Email} via MassTransit SQL Transport", toEmail);
        await _bus.Publish(command);
        _logger.LogInformation("Temporary password email published to {Email}", toEmail);
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? from = null, List<string>? cc = null,
        List<string>? bcc = null, List<EmailAttachmentDto>? attachments = null)
    {
        var defaultFrom = _configuration["SmtpSettings:From"] ?? _configuration["SmtpSettings:Username"] ?? "noreply@example.com";
        
        // Convert attachments to MassTransit format
        List<IEmailAttachment>? mtAttachments = null;
        if (attachments != null)
        {
            mtAttachments = attachments.Select(a => (IEmailAttachment)new EmailAttachment(
                a.FileName, 
                a.Content, 
                a.ContentType)).ToList();
        }

        // Publish to MassTransit SQL Transport for reliable delivery
        var command = new SendEmailCommand
        {
            To = to,
            Subject = subject,
            Body = body,
            IsHtml = isHtml,
            From = from ?? defaultFrom,
            Cc = cc,
            Bcc = bcc,
            Attachments = mtAttachments,
            CorrelationId = Guid.NewGuid().ToString()
        };

        _logger.LogInformation("Publishing email to {Email} with subject: {Subject} via MassTransit SQL Transport", to, subject);
        await _bus.Publish(command);
        _logger.LogInformation("Email published to {Email}", to);
    }
}
