using Application.Commands.Category.CreateCategory;
using Application.Commands.Category.DeleteCategory;
using Application.Commands.Category.UpdateCategory;
using Application.Queries.Catalog.GetCategories;
using Application.Queries.Catalog.GetCategoryAvailableFilters;
using Application.Queries.Catalog.GetCategoryById;
using Application.Queries.Catalog.GetCategoryBySlug;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ControllerBase
{
	private readonly IMediator _mediator;

	public CategoriesController(IMediator mediator)
	{
		_mediator = mediator;
	}

	/// <summary>
	/// Отримати список категорій
	/// </summary>
	[HttpGet]
	[AllowAnonymous]
	[OutputCache(PolicyName = "Categories")]
	public async Task<IActionResult> GetAll([FromQuery] Guid? parentCategoryId = null, [FromQuery] bool topLevelOnly = false)
	{
		var result = await _mediator.Send(new GetCategoriesQuery(parentCategoryId, topLevelOnly));
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати категорію по Id
	/// </summary>
	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	[OutputCache(PolicyName = "Categories")]
	public async Task<IActionResult> GetById([FromRoute] Guid id)
	{
		var result = await _mediator.Send(new GetCategoryByIdQuery(id));
		if (!result.IsSuccess) return NotFound(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати категорію по slug
	/// </summary>
	[HttpGet("slug/{slug}")]
	[AllowAnonymous]
	[OutputCache(PolicyName = "Categories")]
	public async Task<IActionResult> GetBySlug([FromRoute] string slug)
	{
		var result = await _mediator.Send(new GetCategoryBySlugQuery(slug));
		if (!result.IsSuccess) return NotFound(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати доступні фільтри для товарів в категорії
	/// Аналізує реальні дані товарів і повертає тільки ті фільтри, які мають значення
	/// </summary>
	[HttpGet("{id:guid}/available-filters")]
	[AllowAnonymous]
	[OutputCache(Duration = 900)] // Cache for 15 minutes
	public async Task<IActionResult> GetAvailableFilters([FromRoute] Guid id)
	{
		var result = await _mediator.Send(new GetCategoryAvailableFiltersQuery(id));
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Створити категорію
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "Permission:categories.manage")]
	public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
	{
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Оновити категорію
	/// </summary>
	[HttpPut("{id:guid}")]
	[Authorize(Policy = "Permission:categories.manage")]
	public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCategoryRequest request)
	{
		var command = new UpdateCategoryCommand(id, request.Name, request.Description, request.Emoji, request.ParentCategoryId);
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Видалити категорію
	/// </summary>
	[HttpDelete("{id:guid}")]
	[Authorize(Policy = "Permission:categories.manage")]
	public async Task<IActionResult> Delete([FromRoute] Guid id)
	{
		var result = await _mediator.Send(new DeleteCategoryCommand(id));
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}
}

public sealed record UpdateCategoryRequest(string Name, string? Description = null, string? Emoji = null, Guid? ParentCategoryId = null);
