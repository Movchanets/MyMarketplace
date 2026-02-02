using MediatR;
using Application.Interfaces;

namespace Application.Commands.User.SendTestEmail;

public sealed class SendTestEmailHandler(IEmailService emailService) : IRequestHandler<SendTestEmailCommand>
{
	public async Task Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
	{
		var callbackUrl = $"{request.Origin}/test-email?to={System.Net.WebUtility.UrlEncode(request.Email)}&id={System.Guid.NewGuid()}";
		var body = $"Please use the following link to test email functionality:\n\n{callbackUrl}\n\nIf you didn't request this, ignore this email.";
		
		// Send email via IEmailService (which now uses MassTransit SQL Transport internally)
		await emailService.SendEmailAsync(
			to: request.Email,
			subject: "Test Email",
			body: body,
			isHtml: false);
	}
}
