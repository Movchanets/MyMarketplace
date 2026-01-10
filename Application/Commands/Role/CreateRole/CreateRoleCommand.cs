using Application.DTOs;
using MediatR;

namespace Application.Commands.Role.CreateRole;

public record CreateRoleCommand(
    string Name,
    string Description,
    List<string> Permissions
) : IRequest<ServiceResponse<RoleDto>>;
