using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.AttributeDefinitions;

public sealed class DeleteAttributeDefinitionCommandHandler
	: IRequestHandler<DeleteAttributeDefinitionCommand, ServiceResponse>
{
	private readonly IAttributeDefinitionRepository _repository;
	private readonly ILogger<DeleteAttributeDefinitionCommandHandler> _logger;

	public DeleteAttributeDefinitionCommandHandler(
		IAttributeDefinitionRepository repository,
		ILogger<DeleteAttributeDefinitionCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(
		DeleteAttributeDefinitionCommand request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting attribute definition: {Id}", request.Id);

		try
		{
			var definition = await _repository.GetByIdAsync(request.Id);
			if (definition is null)
			{
				_logger.LogWarning("Attribute definition {Id} not found", request.Id);
				return new ServiceResponse(false, "Attribute definition not found");
			}

			await _repository.DeleteAsync(request.Id);

			_logger.LogInformation("Deleted attribute definition {Code}", definition.Code);
			return new ServiceResponse(true, "Attribute definition deleted");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting attribute definition");
			return new ServiceResponse(false, "Failed to delete attribute definition");
		}
	}
}
