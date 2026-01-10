using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Tag.DeleteTag;

public sealed record DeleteTagCommand(Guid Id) : IRequest<ServiceResponse>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["tags", "products"];
}
