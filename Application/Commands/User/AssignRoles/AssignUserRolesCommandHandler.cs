using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Commands.User.AssignRoles;

public class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand, ServiceResponse<AdminUserDto>>
{
    private readonly IAdminUserService _adminUserService;

    public AssignUserRolesCommandHandler(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    public async Task<ServiceResponse<AdminUserDto>> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
    {
        var (success, message, user) = await _adminUserService.AssignUserRolesAsync(
            request.UserId, 
            request.Roles, 
            cancellationToken);
        
        return success
            ? new ServiceResponse<AdminUserDto>(true, message, user)
            : new ServiceResponse<AdminUserDto>(false, message);
    }
}
