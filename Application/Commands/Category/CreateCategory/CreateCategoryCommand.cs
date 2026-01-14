using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Category.CreateCategory;

public sealed record CreateCategoryCommand(
	string Name,
	string? Description = null,
	string? Emoji = null,
	Guid? ParentCategoryId = null
) : IRequest<ServiceResponse<Guid>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["categories"];
}
