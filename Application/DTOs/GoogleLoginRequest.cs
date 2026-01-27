namespace Application.DTOs;

/// <summary>
/// Request для Google login
/// </summary>
public record GoogleLoginRequest(
	string IdToken,
	string? TurnstileToken = null
);
