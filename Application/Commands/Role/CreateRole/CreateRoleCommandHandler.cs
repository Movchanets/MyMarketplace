using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Commands.Role.CreateRole;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, ServiceResponse<RoleDto>>
{
    private readonly IRoleService _roleService;

    public CreateRoleCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<ServiceResponse<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var (success, message, role) = await _roleService.CreateRoleAsync(request.Name, request.Description, request.Permissions);
        
        return success
            ? new ServiceResponse<RoleDto>(true, message, role)
            : new ServiceResponse<RoleDto>(false, message);
    }
}
