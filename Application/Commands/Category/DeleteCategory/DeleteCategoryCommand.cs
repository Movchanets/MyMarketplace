using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Category.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<ServiceResponse>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["categories", "products"];
}
