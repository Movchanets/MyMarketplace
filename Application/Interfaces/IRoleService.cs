using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service for managing roles and permissions
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Get all roles with their permissions and user counts
    /// </summary>
    Task<List<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a role by its ID
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new role with specified permissions
    /// </summary>
    Task<(bool Success, string Message, RoleDto? Role)> CreateRoleAsync(string name, string description, List<string> permissions);

    /// <summary>
    /// Update an existing role
    /// </summary>
    Task<(bool Success, string Message, RoleDto? Role)> UpdateRoleAsync(Guid roleId, string? name, string? description, List<string>? permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a role
    /// </summary>
    Task<(bool Success, string Message)> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a role exists by name
    /// </summary>
    Task<bool> RoleExistsAsync(string roleName);
}

/// <summary>
/// Service for admin user management
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Get all users with detailed admin information
    /// </summary>
    Task<List<AdminUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    Task<(bool Success, string Message, AdminUserDto? User)> AssignUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lock or unlock a user account
    /// </summary>
    Task<(bool Success, string Message)> SetUserLockoutAsync(Guid userId, bool locked, DateTime? lockUntil = null);
}
