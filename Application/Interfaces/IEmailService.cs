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
}
