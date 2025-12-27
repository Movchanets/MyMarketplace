using Application.DTOs;
using MediatR;

namespace Application.Queries.User.GetAdminUsers;

public record GetAdminUsersQuery : IRequest<ServiceResponse<List<AdminUserDto>>>;
