using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.Role.GetRoleById;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, ServiceResponse<RoleDto>>
{
    private readonly IRoleService _roleService;

    public GetRoleByIdQueryHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<ServiceResponse<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetRoleByIdAsync(request.RoleId, cancellationToken);

        if (role == null)
        {
            return new ServiceResponse<RoleDto>(false, "Role not found");
        }

        return new ServiceResponse<RoleDto>(true, "Role retrieved successfully", role);
    }
}
