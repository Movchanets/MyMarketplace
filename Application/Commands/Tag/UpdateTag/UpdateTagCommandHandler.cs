using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Tag.UpdateTag;

public sealed class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, ServiceResponse>
{
	private readonly ITagRepository _tagRepository;
	private readonly ILogger<UpdateTagCommandHandler> _logger;

	public UpdateTagCommandHandler(ITagRepository tagRepository, ILogger<UpdateTagCommandHandler> logger)
	{
		_tagRepository = tagRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating tag {TagId}", request.Id);

		var tag = await _tagRepository.GetByIdAsync(request.Id);
		if (tag is null)
		{
			_logger.LogWarning("Tag {TagId} not found", request.Id);
			return new ServiceResponse(false, "Tag not found");
		}

		// Check for duplicate name (excluding current)
		var existingByName = await _tagRepository.GetByNameAsync(request.Name);
		if (existingByName is not null && existingByName.Id != request.Id)
		{
			_logger.LogWarning("Tag with name {Name} already exists", request.Name);
			return new ServiceResponse(false, $"Tag with name '{request.Name}' already exists");
		}

		tag.Update(request.Name, request.Description);
		await _tagRepository.SaveChangesAsync();

		_logger.LogInformation("Tag {TagId} updated successfully", request.Id);
		return new ServiceResponse(true, "Tag updated successfully");
	}
}
