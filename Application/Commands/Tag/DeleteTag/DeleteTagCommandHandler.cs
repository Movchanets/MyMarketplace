using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Tag.DeleteTag;

public sealed class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand, ServiceResponse>
{
	private readonly ITagRepository _tagRepository;
	private readonly ILogger<DeleteTagCommandHandler> _logger;

	public DeleteTagCommandHandler(ITagRepository tagRepository, ILogger<DeleteTagCommandHandler> logger)
	{
		_tagRepository = tagRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting tag {TagId}", request.Id);

		var tag = await _tagRepository.GetByIdAsync(request.Id);
		if (tag is null)
		{
			_logger.LogWarning("Tag {TagId} not found", request.Id);
			return new ServiceResponse(false, "Tag not found");
		}

		await _tagRepository.DeleteAsync(tag);
		await _tagRepository.SaveChangesAsync();

		_logger.LogInformation("Tag {TagId} deleted successfully", request.Id);
		return new ServiceResponse(true, "Tag deleted successfully");
	}
}
