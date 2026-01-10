using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.Role.GetRoles;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, ServiceResponse<List<RoleDto>>>
{
    private readonly IRoleService _roleService;

    public GetRolesQueryHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<ServiceResponse<List<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllRolesAsync(cancellationToken);
        return new ServiceResponse<List<RoleDto>>(true, "Roles retrieved successfully", roles);
    }
}
