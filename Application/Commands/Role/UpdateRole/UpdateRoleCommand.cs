using Application.DTOs;
using MediatR;

namespace Application.Commands.Role.UpdateRole;

public record UpdateRoleCommand(
    Guid RoleId,
    string? Name,
    string? Description,
    List<string>? Permissions
) : IRequest<ServiceResponse<RoleDto>>;
