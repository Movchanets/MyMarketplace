using Application.Commands.AttributeDefinitions;
using Application.DTOs;
using Application.Queries.AttributeDefinitions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttributeDefinitionsController : ControllerBase
{
	private readonly IMediator _mediator;

	public AttributeDefinitionsController(IMediator mediator)
	{
		_mediator = mediator;
	}

	/// <summary>
	/// Get all attribute definitions
	/// </summary>
	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
	{
		var result = await _mediator.Send(new GetAllAttributeDefinitionsQuery(includeInactive));
		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}
		return Ok(result);
	}

	/// <summary>
	/// Create a new attribute definition (Admin only)
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "Permission:manage_catalog")]
	public async Task<IActionResult> Create([FromBody] CreateAttributeDefinitionRequest request)
	{
		var command = new CreateAttributeDefinitionCommand(
			request.Code,
			request.Name,
			request.DataType,
			request.IsRequired,
			request.IsVariant,
			request.Description,
			request.Unit,
			request.DisplayOrder,
			request.AllowedValues
		);

		var result = await _mediator.Send(command);
		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}
		return CreatedAtAction(nameof(GetAll), new { id = result.Payload }, result);
	}

	/// <summary>
	/// Update an attribute definition (Admin only)
	/// </summary>
	[HttpPut("{id:guid}")]
	[Authorize(Policy = "Permission:manage_catalog")]
	public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttributeDefinitionRequest request)
	{
		var command = new UpdateAttributeDefinitionCommand(
			id,
			request.Name,
			request.DataType,
			request.IsRequired,
			request.IsVariant,
			request.Description,
			request.Unit,
			request.DisplayOrder,
			request.AllowedValues
		);

		var result = await _mediator.Send(command);
		if (!result.IsSuccess)
		{
			return BadRequest(result);
		}
		return Ok(result);
	}

	/// <summary>
	/// Delete an attribute definition (Admin only)
	/// </summary>
	[HttpDelete("{id:guid}")]
	[Authorize(Policy = "Permission:manage_catalog")]
	public async Task<IActionResult> Delete(Guid id)
	{
		var result = await _mediator.Send(new DeleteAttributeDefinitionCommand(id));
		if (!result.IsSuccess)
		{
			return NotFound(result);
		}
		return Ok(result);
	}
}
