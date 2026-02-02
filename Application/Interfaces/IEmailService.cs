using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Send a password reset email containing a callback URL with a token.
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string callbackUrl);

    /// <summary>
    /// Send a temporary password email to user
    /// </summary>
    Task SendTemporaryPasswordEmailAsync(string toEmail, string temporaryPassword);

    /// <summary>
    /// Send a generic email with full control over parameters
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML or plain text)</param>
    /// <param name="isHtml">Whether the body is HTML</param>
    /// <param name="from">Optional sender address</param>
    /// <param name="cc">Optional CC recipients</param>
    /// <param name="bcc">Optional BCC recipients</param>
    /// <param name="attachments">Optional attachments</param>
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        string? from = null,
        List<string>? cc = null,
        List<string>? bcc = null,
        List<EmailAttachmentDto>? attachments = null);
}

/// <summary>
/// DTO for email attachments
/// </summary>
public record EmailAttachmentDto(
    string FileName,
    byte[] Content,
    string ContentType);
