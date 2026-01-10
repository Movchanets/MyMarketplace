using Application.DTOs;
using MediatR;

namespace Application.Commands.User.LockUser;

public record LockUserCommand(
    Guid UserId,
    bool Lock,
    DateTime? LockUntil = null
) : IRequest<ServiceResponse>;
