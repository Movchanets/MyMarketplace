using Application.DTOs;
using MediatR;

namespace Application.Commands.User.AssignRoles;

public record AssignUserRolesCommand(
    Guid UserId,
    List<string> Roles
) : IRequest<ServiceResponse<AdminUserDto>>;
