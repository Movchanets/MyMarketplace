using System.Net;
using System.Net.Mail;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string callbackUrl)
    {
        // Read settings via indexer to avoid requiring the configuration binder package
        var host = _configuration["SmtpSettings:Host"];
        var port = int.TryParse(_configuration["SmtpSettings:Port"], out var p) ? p : 25;
        var user = _configuration["SmtpSettings:Username"];
        var pass = _configuration["SmtpSettings:Password"];
        var from = _configuration["SmtpSettings:From"] ?? user ?? "noreply@example.com";
        var enableSsl = !bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out var ssl) || ssl;

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

        var fromName = _configuration["SmtpSettings:FromName"] ?? from;
        var fromAddress = new MailAddress(from, fromName);

        using var message = new MailMessage();
        message.From = fromAddress;
        message.To.Add(toEmail);
        message.Subject = "Password reset";
        message.Body = body;
        message.IsBodyHtml = isHtml;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
        };

        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
        {
            client.Credentials = new NetworkCredential(user, pass);
        }

        try
        {
            _logger.LogInformation("Sending password reset email to {Email} via SMTP host {Host}", toEmail, host);
            await client.SendMailAsync(message);
            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }
        catch (System.Exception ex)
        {
            // Log without exposing sensitive token content
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendTemporaryPasswordEmailAsync(string toEmail, string temporaryPassword)
    {
        var host = _configuration["SmtpSettings:Host"];
        var port = int.TryParse(_configuration["SmtpSettings:Port"], out var p) ? p : 25;
        var user = _configuration["SmtpSettings:Username"];
        var pass = _configuration["SmtpSettings:Password"];
        var from = _configuration["SmtpSettings:From"] ?? user ?? "noreply@example.com";
        var enableSsl = !bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out var ssl) || ssl;

        var body = $"Ваш тимчасовий пароль для входу: {temporaryPassword}\n\nБудь ласка, змініть його після першого входу.";
        var fromName = _configuration["SmtpSettings:FromName"] ?? from;
        var fromAddress = new MailAddress(from, fromName);

        using var message = new MailMessage();
        message.From = fromAddress;
        message.To.Add(toEmail);
        message.Subject = "Тимчасовий пароль";
        message.Body = body;
        message.IsBodyHtml = false;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
        };

        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
        {
            client.Credentials = new NetworkCredential(user, pass);
        }

        try
        {
            _logger.LogInformation("Sending temporary password email to {Email}", toEmail);
            await client.SendMailAsync(message);
            _logger.LogInformation("Temporary password email sent to {Email}", toEmail);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send temporary password email to {Email}", toEmail);
            throw;
        }
    }
}
