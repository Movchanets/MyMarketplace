using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Commands.Role.DeleteRole;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, ServiceResponse>
{
    private readonly IRoleService _roleService;

    public DeleteRoleCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<ServiceResponse> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var (success, message) = await _roleService.DeleteRoleAsync(request.RoleId, cancellationToken);
        return new ServiceResponse(success, message);
    }
}
