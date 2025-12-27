using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Commands.User.LockUser;

public class LockUserCommandHandler : IRequestHandler<LockUserCommand, ServiceResponse>
{
    private readonly IAdminUserService _adminUserService;

    public LockUserCommandHandler(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    public async Task<ServiceResponse> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var (success, message) = await _adminUserService.SetUserLockoutAsync(
            request.UserId, 
            request.Lock, 
            request.LockUntil);
        
        return new ServiceResponse(success, message);
    }
}
