using Application.Commands.Store.CreateStore;
using Application.Commands.Store.SuspendStore;
using Application.Commands.Store.UpdateStore;
using Application.Commands.Store.VerifyStore;
using Application.Queries.Store.GetAllStores;
using Application.Queries.Store.GetMyStore;
using Application.Queries.Store.GetStoreBySlug;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly ILogger<StoresController> _logger;

	public StoresController(IMediator mediator, ILogger<StoresController> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	/// <summary>
	/// Get all stores (Admin only)
	/// </summary>
	[HttpGet]
	[Authorize(Policy = "Permission:stores.manage")]
	public async Task<IActionResult> GetAll([FromQuery] bool includeUnverified = true)
	{
		var result = await _mediator.Send(new GetAllStoresQuery(includeUnverified));
		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}
		return Ok(result);
	}

	/// <summary>
	/// Get current user's store
	/// </summary>
	[HttpGet("my")]
	[Authorize]
	public async Task<IActionResult> GetMyStore()
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized();
		}

		var result = await _mediator.Send(new GetMyStoreQuery(userId.Value));
		return Ok(result);
	}

	/// <summary>
	/// Get store by slug (public)
	/// </summary>
	[HttpGet("slug/{slug}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetBySlug([FromRoute] string slug)
	{
		var result = await _mediator.Send(new GetStoreBySlugQuery(slug));
		if (!result.IsSuccess || result.Payload is null)
		{
			return NotFound(result);
		}
		return Ok(result);
	}

	/// <summary>
	/// Create a new store for current user
	/// </summary>
	[HttpPost]
	[Authorize]
	public async Task<IActionResult> Create([FromBody] CreateStoreRequest request)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized();
		}

		var command = new CreateStoreCommand(userId.Value, request.Name, request.Description);
		var result = await _mediator.Send(command);

		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}

		return Ok(result);
	}

	/// <summary>
	/// Verify a store (Admin only)
	/// </summary>
	[HttpPost("{id:guid}/verify")]
	[Authorize(Policy = "Permission:stores.manage")]
	public async Task<IActionResult> Verify(Guid id)
	{
		var result = await _mediator.Send(new VerifyStoreCommand(id));
		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}
		return Ok(result);
	}

	/// <summary>
	/// Suspend a store (Admin only)
	/// </summary>
	[HttpPost("{id:guid}/suspend")]
	[Authorize(Policy = "Permission:stores.manage")]
	public async Task<IActionResult> Suspend(Guid id)
	{
		var result = await _mediator.Send(new SuspendStoreCommand(id));
		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}
		return Ok(result);
	}

	/// <summary>
	/// Unsuspend a store (Admin only)
	/// </summary>
	[HttpPost("{id:guid}/unsuspend")]
	[Authorize(Policy = "Permission:stores.manage")]
	public async Task<IActionResult> Unsuspend(Guid id)
	{
		var result = await _mediator.Send(new UnsuspendStoreCommand(id));
		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}
		return Ok(result);
	}

	/// <summary>
	/// Update store details (Owner only)
	/// </summary>
	[HttpPut("{id:guid}")]
	[Authorize]
	public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStoreRequest request)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized();
		}

		// Pass userId (IdentityUserId) to verify ownership in handler
		var command = new UpdateStoreCommand(userId.Value, request.Name, request.Description);
		var result = await _mediator.Send(command);

		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}

		return Ok(result);
	}

	private Guid? GetUserId()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
		{
			return null;
		}
		return userId;
	}
}

public record CreateStoreRequest(string Name, string? Description);
public record UpdateStoreRequest(string Name, string? Description);
