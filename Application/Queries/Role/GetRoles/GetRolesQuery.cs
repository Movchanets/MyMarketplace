using Application.DTOs;
using MediatR;

namespace Application.Queries.Role.GetRoles;

public record GetRolesQuery : IRequest<ServiceResponse<List<RoleDto>>>;
