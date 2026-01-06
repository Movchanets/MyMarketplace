using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Tag.CreateTag;

public sealed record CreateTagCommand(
	string Name,
	string? Description = null
) : IRequest<ServiceResponse<Guid>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["tags"];
}
