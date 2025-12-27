using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Commands.Role.UpdateRole;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, ServiceResponse<RoleDto>>
{
    private readonly IRoleService _roleService;

    public UpdateRoleCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<ServiceResponse<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var (success, message, role) = await _roleService.UpdateRoleAsync(
            request.RoleId, 
            request.Name, 
            request.Description, 
            request.Permissions, 
            cancellationToken);
        
        return success
            ? new ServiceResponse<RoleDto>(true, message, role)
            : new ServiceResponse<RoleDto>(false, message);
    }
}
