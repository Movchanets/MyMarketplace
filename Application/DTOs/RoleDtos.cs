namespace Application.DTOs;

/// <summary>
/// DTO for role information
/// </summary>
public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    List<string> Permissions,
    int UsersCount
);

/// <summary>
/// DTO for creating a new role
/// </summary>
public record CreateRoleDto(
    string Name,
    string Description,
    List<string> Permissions
);

/// <summary>
/// DTO for updating an existing role
/// </summary>
public record UpdateRoleDto(
    string? Name,
    string? Description,
    List<string>? Permissions
);

/// <summary>
/// DTO for assigning roles to a user
/// </summary>
public record AssignUserRolesDto(
    List<string> Roles
);

/// <summary>
/// Extended user DTO for admin management
/// </summary>
public record AdminUserDto(
    Guid Id,
    string Username,
    string Name,
    string Surname,
    string Email,
    string PhoneNumber,
    List<string> Roles,
    string? AvatarUrl,
    bool IsEmailConfirmed,
    bool IsLocked,
    DateTime? LockoutEnd,
    DateTime CreatedAt
);

/// <summary>
/// DTO for permission info
/// </summary>
public record PermissionDto(
    string Name,
    string Description,
    string Category
);
