using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Queries.User.GetAdminUsers;

public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, ServiceResponse<List<AdminUserDto>>>
{
    private readonly IAdminUserService _adminUserService;

    public GetAdminUsersQueryHandler(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    public async Task<ServiceResponse<List<AdminUserDto>>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _adminUserService.GetAllUsersAsync(cancellationToken);
        return new ServiceResponse<List<AdminUserDto>>(true, "Users retrieved successfully", users);
    }
}
