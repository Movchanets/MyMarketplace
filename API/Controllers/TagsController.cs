using Application.Commands.Tag.CreateTag;
using Application.Commands.Tag.DeleteTag;
using Application.Commands.Tag.UpdateTag;
using Application.Queries.Catalog.GetTagById;
using Application.Queries.Catalog.GetTagBySlug;
using Application.Queries.Catalog.GetTags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class TagsController : ControllerBase
{
	private readonly IMediator _mediator;

	public TagsController(IMediator mediator)
	{
		_mediator = mediator;
	}

	/// <summary>
	/// Отримати список тегів
	/// </summary>
	[HttpGet]
	[AllowAnonymous]
	[OutputCache(PolicyName = "Tags")]
	public async Task<IActionResult> GetAll()
	{
		var result = await _mediator.Send(new GetTagsQuery());
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати тег по Id
	/// </summary>
	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	[OutputCache(PolicyName = "Tags")]
	public async Task<IActionResult> GetById([FromRoute] Guid id)
	{
		var result = await _mediator.Send(new GetTagByIdQuery(id));
		if (!result.IsSuccess) return NotFound(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати тег по slug
	/// </summary>
	[HttpGet("slug/{slug}")]
	[AllowAnonymous]
	[OutputCache(PolicyName = "Tags")]
	public async Task<IActionResult> GetBySlug([FromRoute] string slug)
	{
		var result = await _mediator.Send(new GetTagBySlugQuery(slug));
		if (!result.IsSuccess) return NotFound(result);
		return Ok(result);
	}

	/// <summary>
	/// Створити тег
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "Permission:tags.manage")]
	public async Task<IActionResult> Create([FromBody] CreateTagCommand command)
	{
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Оновити тег
	/// </summary>
	[HttpPut("{id:guid}")]
	[Authorize(Policy = "Permission:tags.manage")]
	public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTagRequest request)
	{
		var command = new UpdateTagCommand(id, request.Name, request.Description);
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Видалити тег
	/// </summary>
	[HttpDelete("{id:guid}")]
	[Authorize(Policy = "Permission:tags.manage")]
	public async Task<IActionResult> Delete([FromRoute] Guid id)
	{
		var result = await _mediator.Send(new DeleteTagCommand(id));
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}
}

public sealed record UpdateTagRequest(string Name, string? Description = null);
