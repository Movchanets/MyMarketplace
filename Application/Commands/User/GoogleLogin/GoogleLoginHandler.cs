using System.Security.Cryptography;
using Application.DTOs;
using Application.Interfaces;
using Application.Models;
using Domain.Constants;
using Google.Apis.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Commands.User.GoogleLogin;

/// <summary>
/// Handler для Google login
/// </summary>
public sealed class GoogleLoginHandler : IRequestHandler<GoogleLoginCommand, TokenResponse>
{
	private readonly IUserService _userService;
	private readonly ITokenService _tokenService;
	private readonly IEmailService _emailService;
	private readonly IConfiguration _configuration;
	private readonly ILogger<GoogleLoginHandler> _logger;

	public GoogleLoginHandler(
		IUserService userService,
		ITokenService tokenService,
		IEmailService emailService,
		IConfiguration configuration,
		ILogger<GoogleLoginHandler> logger)
	{
		_userService = userService;
		_tokenService = tokenService;
		_emailService = emailService;
		_configuration = configuration;
		_logger = logger;
	}

	public async Task<TokenResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
	{
		var googleClientId = _configuration["GoogleAuth:ClientId"];
		if (string.IsNullOrEmpty(googleClientId))
		{
			_logger.LogError("Google ClientId is not configured");
			throw new InvalidOperationException("Google authentication is not configured");
		}

		GoogleJsonWebSignature.Payload payload;
		try
		{
			// Verify the Google ID token
			payload = await GoogleJsonWebSignature.ValidateAsync(request.Request.IdToken);

			if (payload.Audience.ToString() != googleClientId)
			{
				_logger.LogWarning("Invalid Google token audience");
				throw new UnauthorizedAccessException("Invalid Google token");
			}
		}
		catch (InvalidJwtException ex)
		{
			_logger.LogError(ex, "Invalid Google JWT token");
			throw new UnauthorizedAccessException("Invalid Google token");
		}

		var email = payload.Email;
		if (!payload.EmailVerified)
		{
			_logger.LogWarning("Google email not verified for {Email}", email);
			throw new UnauthorizedAccessException("Google email is not verified");
		}

		// Check if user exists
		var existingUser = await _userService.FindUserByEmailAsync(email);

		if (existingUser != null)
		{
			// User exists, return tokens
			_logger.LogInformation("User {Email} logged in via Google", email);
			return await _tokenService.GenerateTokensAsync(existingUser.IdentityUserId);
		}

		// User doesn't exist, create new account
		_logger.LogInformation("Creating new user from Google login: {Email}", email);

		// Generate a random temporary password
		var temporaryPassword = GenerateTemporaryPassword();

		var registrationDto = new RegistrationDto
		{
			Email = email,
			Password = temporaryPassword,
			ConfirmPassword = temporaryPassword,
			Name = payload.GivenName ?? string.Empty,
			Surname = payload.FamilyName ?? string.Empty
		};

		var registrationResult = await _userService.RegisterAsync(registrationDto);

		if (!registrationResult.Succeeded)
		{
			_logger.LogError("Failed to create user from Google login: {Email}, Errors: {Errors}",
				email, string.Join(", ", registrationResult.Errors));
			throw new InvalidOperationException("Failed to create user account");
		}

		// Assign User role
		await _userService.EnsureRoleExistsAsync(Roles.User);
		if (registrationResult.IdentityUserId.HasValue)
		{
			await _userService.AddUserToRoleAsync(registrationResult.IdentityUserId.Value, Roles.User);
		}

		// Send temporary password email
		try
		{
			await _emailService.SendTemporaryPasswordEmailAsync(email, temporaryPassword);
			_logger.LogInformation("Temporary password sent to {Email}", email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send temporary password email to {Email}", email);
			// Don't throw - user is created, they can reset password later
		}

		// Generate and return tokens
		var newUser = await _userService.FindUserByEmailAsync(email);
		if (newUser == null)
		{
			_logger.LogError("User not found after registration: {Email}", email);
			throw new InvalidOperationException("User creation failed");
		}

		_logger.LogInformation("New user created via Google login: {Email}", email);
		return await _tokenService.GenerateTokensAsync(newUser.IdentityUserId);
	}

	private static string GenerateTemporaryPassword()
	{
		const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
		const string digits = "0123456789";
		const string specialChars = "!@#$%^&*";
		const string allChars = upperCase + lowerCase + digits + specialChars;

		var password = new char[16];

		// Ensure password contains at least one of each required character type
		password[0] = upperCase[RandomNumberGenerator.GetInt32(upperCase.Length)];
		password[1] = lowerCase[RandomNumberGenerator.GetInt32(lowerCase.Length)];
		password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
		password[3] = specialChars[RandomNumberGenerator.GetInt32(specialChars.Length)];

		// Fill the rest randomly
		for (int i = 4; i < password.Length; i++)
		{
			password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
		}

		// Shuffle the password
		for (int i = password.Length - 1; i > 0; i--)
		{
			int j = RandomNumberGenerator.GetInt32(i + 1);
			(password[i], password[j]) = (password[j], password[i]);
		}

		return new string(password);
	}
}
