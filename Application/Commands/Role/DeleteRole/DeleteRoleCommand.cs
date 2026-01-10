using Application.DTOs;
using MediatR;

namespace Application.Commands.Role.DeleteRole;

public record DeleteRoleCommand(Guid RoleId) : IRequest<ServiceResponse>;
