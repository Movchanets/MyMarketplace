using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for admin user management operations
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<RoleEntity> _roleManager;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<RoleEntity> roleManager,
        IFileStorage fileStorage,
        ILogger<AdminUserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<List<AdminUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .Include(u => u.DomainUser)
                .ThenInclude(d => d!.Avatar)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.DomainUser != null ? u.DomainUser.CreatedAt : DateTime.MinValue)
            .ToListAsync(cancellationToken);

        var adminUsers = new List<AdminUserDto>();

        foreach (var user in users)
        {
            var domain = user.DomainUser;
            var roles = user.UserRoles.Select(ur => ur.Role.Name ?? string.Empty).ToList();

            string? avatarUrl = null;
            if (domain?.Avatar != null && !string.IsNullOrEmpty(domain.Avatar.StorageKey))
            {
                avatarUrl = _fileStorage.GetPublicUrl(domain.Avatar.StorageKey);
            }

            adminUsers.Add(new AdminUserDto(
                user.Id,
                user.UserName ?? string.Empty,
                domain?.Name ?? string.Empty,
                domain?.Surname ?? string.Empty,
                user.Email ?? string.Empty,
                user.PhoneNumber ?? string.Empty,
                roles,
                avatarUrl,
                user.EmailConfirmed,
                user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                user.LockoutEnd?.UtcDateTime,
                domain?.CreatedAt ?? DateTime.MinValue
            ));
        }

        return adminUsers;
    }

    public async Task<(bool Success, string Message, AdminUserDto? User)> AssignUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .Include(u => u.DomainUser)
                .ThenInclude(d => d!.Avatar)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return (false, "User not found", null);
        }

        // Validate all roles exist
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return (false, $"Role '{roleName}' does not exist", null);
            }
        }

        // Get current roles
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Remove roles that are not in the new list
        var rolesToRemove = currentRoles.Except(roles).ToList();
        if (rolesToRemove.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                return (false, $"Failed to remove roles: {errors}", null);
            }
        }

        // Add new roles
        var rolesToAdd = roles.Except(currentRoles).ToList();
        if (rolesToAdd.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return (false, $"Failed to add roles: {errors}", null);
            }
        }

        _logger.LogInformation("Updated roles for user {UserId}: {Roles}", userId, string.Join(", ", roles));

        // Update security stamp to invalidate existing tokens
        await _userManager.UpdateSecurityStampAsync(user);

        // Reload user data
        user = await _userManager.Users
            .Include(u => u.DomainUser)
                .ThenInclude(d => d!.Avatar)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var domain = user!.DomainUser;
        var userRoles = user.UserRoles.Select(ur => ur.Role.Name ?? string.Empty).ToList();

        string? avatarUrl = null;
        if (domain?.Avatar != null && !string.IsNullOrEmpty(domain.Avatar.StorageKey))
        {
            avatarUrl = _fileStorage.GetPublicUrl(domain.Avatar.StorageKey);
        }

        var adminUserDto = new AdminUserDto(
            user.Id,
            user.UserName ?? string.Empty,
            domain?.Name ?? string.Empty,
            domain?.Surname ?? string.Empty,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            userRoles,
            avatarUrl,
            user.EmailConfirmed,
            user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            user.LockoutEnd?.UtcDateTime,
            domain?.CreatedAt ?? DateTime.MinValue
        );

        return (true, "User roles updated successfully", adminUserDto);
    }

    public async Task<(bool Success, string Message)> SetUserLockoutAsync(Guid userId, bool locked, DateTime? lockUntil = null)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, "User not found");
        }

        if (locked)
        {
            // Lock the user
            var lockEnd = lockUntil ?? DateTime.UtcNow.AddYears(100); // Permanent lock if no date specified
            var result = await _userManager.SetLockoutEndDateAsync(user, new DateTimeOffset(lockEnd, TimeSpan.Zero));

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Failed to lock user: {errors}");
            }

            _logger.LogInformation("User {UserId} locked until {LockUntil}", userId, lockEnd);
            return (true, $"User locked until {lockEnd:yyyy-MM-dd HH:mm}");
        }
        else
        {
            // Unlock the user
            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Failed to unlock user: {errors}");
            }

            _logger.LogInformation("User {UserId} unlocked", userId);
            return (true, "User unlocked successfully");
        }
    }
}
