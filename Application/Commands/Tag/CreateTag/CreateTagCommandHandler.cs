using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using TagEntity = Domain.Entities.Tag;

namespace Application.Commands.Tag.CreateTag;

public sealed class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, ServiceResponse<Guid>>
{
	private readonly ITagRepository _tagRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<CreateTagCommandHandler> _logger;

	public CreateTagCommandHandler(
		ITagRepository tagRepository,
		IUnitOfWork unitOfWork,
		ILogger<CreateTagCommandHandler> logger)
	{
		_tagRepository = tagRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<Guid>> Handle(CreateTagCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Creating tag {TagName}", request.Name);

		try
		{
			var tag = TagEntity.Create(request.Name, request.Description);

			var existingBySlug = await _tagRepository.GetBySlugAsync(tag.Slug);
			if (existingBySlug != null)
			{
				_logger.LogWarning("Tag slug already exists {Slug}", tag.Slug);
				return new ServiceResponse<Guid>(false, "Tag with same slug already exists");
			}

			_tagRepository.Add(tag);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Tag {TagId} created successfully", tag.Id);
			return new ServiceResponse<Guid>(true, "Tag created successfully", tag.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating tag {TagName}", request.Name);
			return new ServiceResponse<Guid>(false, $"Error: {ex.Message}");
		}
	}
}
