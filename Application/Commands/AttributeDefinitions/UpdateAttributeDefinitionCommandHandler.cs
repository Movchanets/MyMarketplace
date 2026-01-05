using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.AttributeDefinitions;

public sealed class UpdateAttributeDefinitionCommandHandler
	: IRequestHandler<UpdateAttributeDefinitionCommand, ServiceResponse>
{
	private readonly IAttributeDefinitionRepository _repository;
	private readonly ILogger<UpdateAttributeDefinitionCommandHandler> _logger;

	public UpdateAttributeDefinitionCommandHandler(
		IAttributeDefinitionRepository repository,
		ILogger<UpdateAttributeDefinitionCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(
		UpdateAttributeDefinitionCommand request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating attribute definition: {Id}", request.Id);

		try
		{
			var definition = await _repository.GetByIdAsync(request.Id);
			if (definition is null)
			{
				_logger.LogWarning("Attribute definition {Id} not found", request.Id);
				return new ServiceResponse(false, "Attribute definition not found");
			}

			definition.Update(
				request.Name,
				request.DataType,
				request.IsRequired,
				request.IsVariant,
				request.Description,
				request.Unit,
				request.DisplayOrder
			);

			if (request.AllowedValues is not null)
			{
				definition.SetAllowedValues(request.AllowedValues);
			}

			await _repository.UpdateAsync(definition);

			_logger.LogInformation("Updated attribute definition {Code}", definition.Code);
			return new ServiceResponse(true, "Attribute definition updated");
		}
		catch (ArgumentException ex)
		{
			_logger.LogWarning(ex, "Invalid argument when updating attribute definition");
			return new ServiceResponse(false, ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating attribute definition");
			return new ServiceResponse(false, "Failed to update attribute definition");
		}
	}
}
