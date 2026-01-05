using Application.Commands.Role.CreateRole;
using Application.Commands.Role.DeleteRole;
using Application.Commands.Role.UpdateRole;
using Application.Commands.User.AssignRoles;
using Application.Commands.User.LockUser;
using Application.DTOs;
using Application.Queries.Role.GetAllPermissions;
using Application.Queries.Role.GetRoleById;
using Application.Queries.Role.GetRoles;
using Application.Queries.User.GetAdminUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMediator mediator, ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    #region Roles Management

    /// <summary>
    /// Get all roles with their permissions
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Policy = "Permission:roles.read")]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _mediator.Send(new GetRolesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("roles/{id}")]
    [Authorize(Policy = "Permission:roles.read")]
    public async Task<IActionResult> GetRole(Guid id)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id));
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Get all available permissions grouped by category
    /// </summary>
    [HttpGet("permissions")]
    [Authorize(Policy = "Permission:roles.read")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var result = await _mediator.Send(new GetAllPermissionsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost("roles")]
    [Authorize(Policy = "Permission:roles.create")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        var result = await _mediator.Send(new CreateRoleCommand(dto.Name, dto.Description, dto.Permissions));
        if (!result.IsSuccess) return BadRequest(result);
        return CreatedAtAction(nameof(GetRole), new { id = result.Payload?.Id }, result);
    }

    /// <summary>
    /// Update an existing role
    /// </summary>
    [HttpPut("roles/{id}")]
    [Authorize(Policy = "Permission:roles.update")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleDto dto)
    {
        var result = await _mediator.Send(new UpdateRoleCommand(id, dto.Name, dto.Description, dto.Permissions));
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    [HttpDelete("roles/{id}")]
    [Authorize(Policy = "Permission:roles.delete")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(id));
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    #endregion

    #region Users Management

    /// <summary>
    /// Get all users with detailed information for admin
    /// </summary>
    [HttpGet("users")]
    [Authorize(Policy = "Permission:users.read")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _mediator.Send(new GetAdminUsersQuery());
        return Ok(result);
    }

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    [HttpPut("users/{id}/roles")]
    [Authorize(Policy = "Permission:users.update")]
    public async Task<IActionResult> AssignUserRoles(Guid id, [FromBody] AssignUserRolesDto dto)
    {
        var result = await _mediator.Send(new AssignUserRolesCommand(id, dto.Roles));
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Lock a user account
    /// </summary>
    [HttpPost("users/{id}/lock")]
    [Authorize(Policy = "Permission:users.update")]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] LockUserDto? dto = null)
    {
        var result = await _mediator.Send(new LockUserCommand(id, true, dto?.LockUntil));
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Unlock a user account
    /// </summary>
    [HttpPost("users/{id}/unlock")]
    [Authorize(Policy = "Permission:users.update")]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var result = await _mediator.Send(new LockUserCommand(id, false));
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    #endregion
}

/// <summary>
/// DTO for locking a user
/// </summary>
public record LockUserDto(DateTime? LockUntil);
