using Application.DTOs;
using Domain.Constants;
using MediatR;

namespace Application.Queries.Role.GetAllPermissions;

public class GetAllPermissionsQueryHandler : IRequestHandler<GetAllPermissionsQuery, ServiceResponse<Dictionary<string, List<PermissionDto>>>>
{
    public Task<ServiceResponse<Dictionary<string, List<PermissionDto>>>> Handle(
        GetAllPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var grouped = Permissions.GetAllGrouped();

        var result = grouped.ToDictionary(
            g => g.Key,
            g => g.Value.Select(p => new PermissionDto(p.Name, p.Description, g.Key)).ToList()
        );

        return Task.FromResult(new ServiceResponse<Dictionary<string, List<PermissionDto>>>(
            true,
            "Permissions retrieved successfully",
            result
        ));
    }
}
