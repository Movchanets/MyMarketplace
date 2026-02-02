using Application.Commands.User.ForgotPassword;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Commands.User.ForgotPassword;

public sealed class ForgotPasswordHandler(
	IUserService identity,
	IMemoryCache memoryCache,
	IEmailService emailService)
	: IRequestHandler<ForgotPasswordCommand>
{
	public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
	{
		// Turnstile token is validated by API filter when present.
		var exists = await identity.EmailExistsAsync(request.Email);
		if (!exists)
			return;

		var cacheKey = $"forgot:{request.Email}";
		var attempts = memoryCache.GetOrCreate(cacheKey, entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
			return 0;
		});

		if (attempts >= 5)
		{
			await identity.UpdateSecurityStampByEmailAsync(request.Email);
			return;
		}

		memoryCache.Set(cacheKey, attempts + 1, TimeSpan.FromHours(1));

		var token = await identity.GeneratePasswordResetTokenAsync(request.Email);
		if (token is null) return;

		var callbackUrl = $"{request.Origin}/reset-password?email={System.Net.WebUtility.UrlEncode(request.Email)}&token={System.Net.WebUtility.UrlEncode(token)}";

		// Send password reset email via IEmailService (which now uses MassTransit SQL Transport internally)
		await emailService.SendPasswordResetEmailAsync(request.Email, callbackUrl);
	}
}
