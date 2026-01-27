using System.Security.Claims;
using Application.Commands.User.AuthenticateUser;
using Application.Commands.User.ForgotPassword;
using Application.Commands.User.GoogleLogin;
using Application.Commands.User.RefreshTokenCommand;
using Application.Commands.User.RegisterUser;
using Application.Commands.User.ResetPassword;
using Application.Commands.User.SendTestEmail;
using Application.Queries.User.CheckEmail;
using Application.DTOs;
using Application.Interfaces;
using Application.Models;
using Infrastructure.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly ITokenService _tokenService;
	private readonly ILogger<AuthController> _logger;

	public AuthController(IMediator mediator, UserManager<ApplicationUser> userManager, ITokenService tokenService, ILogger<AuthController> logger)
	{
		_mediator = mediator;
		_userManager = userManager;
		_tokenService = tokenService;
		_logger = logger;
	}

	[AllowAnonymous]
	[HttpGet("test-tokens")]
	public async Task<IActionResult> GetTokensTest()
	{
		var user = await _userManager.FindByEmailAsync("admin@example.com");
		if (user == null) return NotFound("User not found");
		var tokens = await _tokenService.GenerateTokensAsync(user.Id);
		return Ok(tokens);
	}

	[AllowAnonymous]
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		_logger.LogInformation("Login attempt for email: {Email}", request.Email);

		try
		{
			var tokens = await _mediator.Send(new AuthenticateUserCommand(request));
			_logger.LogInformation("User {Email} logged in successfully", request.Email);
			return Ok(tokens);
		}
		catch (UnauthorizedAccessException)
		{
			_logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
			return Unauthorized(new { Message = "Invalid credentials" });
		}
	}

	[AllowAnonymous]
	[HttpPost("refresh")]
	public async Task<IActionResult> Refresh([FromBody] TokenRequest request)
	{
		try
		{
			var tokens = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken));
			return Ok(tokens);
		}
		catch (UnauthorizedAccessException)
		{
			return Unauthorized();
		}
	}

	[AllowAnonymous]
	[HttpPost("google-login")]
	public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
	{
		_logger.LogInformation("Google login attempt");

		try
		{
			var tokens = await _mediator.Send(new GoogleLoginCommand(request));
			_logger.LogInformation("Google login successful");
			return Ok(tokens);
		}
		catch (UnauthorizedAccessException ex)
		{
			_logger.LogWarning(ex, "Failed Google login attempt");
			return Unauthorized(new { Message = "Invalid Google token" });
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogError(ex, "Google login error");
			return StatusCode(500, new { Message = "Google authentication failed" });
		}
	}

	[AllowAnonymous]
	[HttpPost("register")]
	public async Task<IActionResult> CreateUser([FromBody] RegistrationDto request)
	{
		_logger.LogInformation("Creating new user with email: {Email}", request.Email);

		if (request.Password != request.ConfirmPassword)
		{
			_logger.LogWarning("Password mismatch for user registration: {Email}", request.Email);
			return BadRequest("Password and Confirm Password do not match.");
		}

		var result = await _mediator.Send(new RegisterUserCommand(request));

		if (result.IsSuccess)
		{
			_logger.LogInformation("User created successfully: {Email}", request.Email);
			ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);
			if (user == null)
			{
				_logger.LogError("User not found after creation: {Email}", request.Email);
				return StatusCode(500, "User creation failed.");
			}
			TokenResponse tokens = await _tokenService.GenerateTokensAsync(user.Id);
			return Ok(tokens);
		}

		_logger.LogError("Failed to create user: {Email}. Errors: {Errors}", request.Email, result.Payload);
		return BadRequest(new { Message = "User creation failed", Errors = result.Payload });
	}

	[AllowAnonymous]
	[HttpGet("check-email")]
	public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] string? turnstileToken)
	{
		_logger.LogInformation("Check if email exists: {Email}", email);
		var result = await _mediator.Send(new CheckEmailQuery(email));
		return Ok(result);
	}

	[AllowAnonymous]
	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
	{
		_logger.LogInformation("Forgot password requested for email: {Email}", request.Email);
		var origin = Request.Headers["Origin"].FirstOrDefault() ?? $"{Request.Scheme}://{Request.Host}";
		try
		{
			await _mediator.Send(new ForgotPasswordCommand(request.Email, origin, request.TurnstileToken));
			return Ok(new { Message = "If the email exists, a password reset link will be sent." });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed processing forgot-password for {Email}", request.Email);
			return StatusCode(500, new { Message = "Failed to process request." });
		}
	}

	[AllowAnonymous]
	[HttpGet("send-test-email/{email}")]
	public async Task<IActionResult> SendTestEmail(string email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return BadRequest(new { Message = "Email is required." });
		var origin = Request.Headers["Origin"].FirstOrDefault() ?? $"{Request.Scheme}://{Request.Host}";
		try
		{
			await _mediator.Send(new SendTestEmailCommand(email, origin));
			_logger.LogInformation("Enqueued test email for {Email}", email);
			return Ok(new { Message = "Test email enqueued." });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to enqueue test email for {Email}", email);
			return StatusCode(500, new { Message = "Failed to enqueue email." });
		}
	}

	[AllowAnonymous]
	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
	{
		_logger.LogInformation("Reset password attempt for email: {Email}", request.Email);
		try
		{
			await _mediator.Send(new ResetPasswordCommand(request));
			_logger.LogInformation("Password reset successful for {Email}", request.Email);
			return Ok(new { Message = "Password has been reset successfully." });
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogWarning("Failed to reset password for {Email}. Error: {Error}", request.Email, ex.Message);
			return BadRequest(new { Message = "Failed to reset password.", Errors = new[] { ex.Message } });
		}
	}

}
