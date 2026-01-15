using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Category.UpdateCategory;

public sealed record UpdateCategoryCommand(
	Guid Id,
	string Name,
	string? Description = null,
	string? Emoji = null,
	Guid? ParentCategoryId = null
) : IRequest<ServiceResponse>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["categories"];
}
