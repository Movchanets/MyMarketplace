using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Tag.UpdateTag;

public sealed record UpdateTagCommand(
	Guid Id,
	string Name,
	string? Description = null
) : IRequest<ServiceResponse>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["tags"];
}
