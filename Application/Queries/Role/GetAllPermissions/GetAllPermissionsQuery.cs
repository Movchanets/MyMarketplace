using Application.DTOs;
using Domain.Constants;
using MediatR;

namespace Application.Queries.Role.GetAllPermissions;

public record GetAllPermissionsQuery : IRequest<ServiceResponse<Dictionary<string, List<PermissionDto>>>>;
