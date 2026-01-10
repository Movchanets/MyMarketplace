using Application.DTOs;
using MediatR;

namespace Application.Queries.Role.GetRoleById;

public record GetRoleByIdQuery(Guid RoleId) : IRequest<ServiceResponse<RoleDto>>;
