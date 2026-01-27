using Application.DTOs;
using Application.Models;
using MediatR;

namespace Application.Commands.User.GoogleLogin;

/// <summary>
/// Команда для Google login
/// </summary>
public sealed record GoogleLoginCommand(GoogleLoginRequest Request) : IRequest<TokenResponse>;
