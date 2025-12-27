using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Infrastructure.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing roles and permissions
/// </summary>
public class RoleService : IRoleService
{
    private readonly RoleManager<RoleEntity> _roleManager;
    private readonly ILogger<RoleService> _logger;

    public RoleService(RoleManager<RoleEntity> roleManager, ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<List<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .Include(r => r.UserRoles)
            .ToListAsync(cancellationToken);

        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            roleDtos.Add(new RoleDto(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                permissions,
                role.UserRoles.Count
            ));
        }

        return roleDtos;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null) return null;

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();

        return new RoleDto(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            permissions,
            role.UserRoles.Count
        );
    }

    public async Task<(bool Success, string Message, RoleDto? Role)> CreateRoleAsync(string name, string description, List<string> permissions)
    {
        // Check if role already exists
        var existingRole = await _roleManager.FindByNameAsync(name);
        if (existingRole != null)
        {
            return (false, $"Role '{name}' already exists", null);
        }

        // Validate permissions
        var validPermissions = Permissions.GetAll();
        var invalidPermissions = permissions.Where(p => !validPermissions.Contains(p)).ToList();
        if (invalidPermissions.Any())
        {
            return (false, $"Invalid permissions: {string.Join(", ", invalidPermissions)}", null);
        }

        // Create role
        var role = new RoleEntity
        {
            Name = name,
            Description = description
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create role {RoleName}: {Errors}", name, errors);
            return (false, $"Failed to create role: {errors}", null);
        }

        // Add permissions as claims
        foreach (var permission in permissions)
        {
            var claimResult = await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
            if (!claimResult.Succeeded)
            {
                _logger.LogWarning("Failed to add permission {Permission} to role {RoleName}", permission, name);
            }
        }

        _logger.LogInformation("Role {RoleName} created with {PermissionCount} permissions", name, permissions.Count);

        var roleDto = new RoleDto(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            permissions,
            0
        );

        return (true, "Role created successfully", roleDto);
    }

    public async Task<(bool Success, string Message, RoleDto? Role)> UpdateRoleAsync(Guid roleId, string? name, string? description, List<string>? permissions, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
        {
            return (false, "Role not found", null);
        }

        // Prevent editing built-in Admin role name
        if (role.Name == Roles.Admin && name != null && name != Roles.Admin)
        {
            return (false, "Cannot rename the Admin role", null);
        }

        // Check if new name conflicts with existing role
        if (!string.IsNullOrEmpty(name) && name != role.Name)
        {
            var existingRole = await _roleManager.FindByNameAsync(name);
            if (existingRole != null)
            {
                return (false, $"Role '{name}' already exists", null);
            }
            role.Name = name;
        }

        if (description != null)
        {
            role.Description = description;
        }

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Failed to update role: {errors}", null);
        }

        // Update permissions if provided
        if (permissions != null)
        {
            // Validate permissions
            var validPermissions = Permissions.GetAll();
            var invalidPermissions = permissions.Where(p => !validPermissions.Contains(p)).ToList();
            if (invalidPermissions.Any())
            {
                return (false, $"Invalid permissions: {string.Join(", ", invalidPermissions)}", null);
            }

            // Remove all existing permission claims
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = existingClaims.Where(c => c.Type == "permission").ToList();

            foreach (var claim in permissionClaims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            // Add new permissions
            foreach (var permission in permissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
            }

            _logger.LogInformation("Updated permissions for role {RoleName}: {Permissions}", role.Name, string.Join(", ", permissions));
        }

        // Get updated permissions
        var updatedClaims = await _roleManager.GetClaimsAsync(role);
        var finalPermissions = updatedClaims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();

        var roleDto = new RoleDto(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            finalPermissions,
            role.UserRoles.Count
        );

        return (true, "Role updated successfully", roleDto);
    }

    public async Task<(bool Success, string Message)> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
        {
            return (false, "Role not found");
        }

        // Prevent deleting built-in roles
        var builtInRoles = new[] { Roles.Admin, Roles.User, Roles.Seller };
        if (builtInRoles.Contains(role.Name))
        {
            return (false, $"Cannot delete built-in role '{role.Name}'");
        }

        // Check if role has users assigned
        if (role.UserRoles.Any())
        {
            return (false, $"Cannot delete role '{role.Name}' because it has {role.UserRoles.Count} users assigned");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Failed to delete role: {errors}");
        }

        _logger.LogInformation("Role {RoleName} (ID: {RoleId}) deleted", role.Name, role.Id);

        return (true, "Role deleted successfully");
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }
}
