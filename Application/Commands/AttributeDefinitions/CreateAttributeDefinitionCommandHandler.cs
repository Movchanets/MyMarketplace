using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.AttributeDefinitions;

public sealed class CreateAttributeDefinitionCommandHandler
	: IRequestHandler<CreateAttributeDefinitionCommand, ServiceResponse<Guid>>
{
	private readonly IAttributeDefinitionRepository _repository;
	private readonly ILogger<CreateAttributeDefinitionCommandHandler> _logger;

	public CreateAttributeDefinitionCommandHandler(
		IAttributeDefinitionRepository repository,
		ILogger<CreateAttributeDefinitionCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<ServiceResponse<Guid>> Handle(
		CreateAttributeDefinitionCommand request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Creating attribute definition: {Code}", request.Code);

		try
		{
			// Check for duplicate code
			if (await _repository.ExistsAsync(request.Code))
			{
				_logger.LogWarning("Attribute definition with code {Code} already exists", request.Code);
				return new ServiceResponse<Guid>(false, $"Attribute with code '{request.Code}' already exists", Guid.Empty);
			}

			var definition = new AttributeDefinition(
				request.Code,
				request.Name,
				request.DataType,
				request.IsRequired,
				request.IsVariant,
				request.Description,
				request.Unit,
				request.DisplayOrder
			);

			if (request.AllowedValues is not null && request.AllowedValues.Count > 0)
			{
				definition.SetAllowedValues(request.AllowedValues);
			}

			await _repository.AddAsync(definition);

			_logger.LogInformation("Created attribute definition {Code} with Id {Id}", definition.Code, definition.Id);
			return new ServiceResponse<Guid>(true, "Attribute definition created", definition.Id);
		}
		catch (ArgumentException ex)
		{
			_logger.LogWarning(ex, "Invalid argument when creating attribute definition");
			return new ServiceResponse<Guid>(false, ex.Message, Guid.Empty);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating attribute definition");
			return new ServiceResponse<Guid>(false, "Failed to create attribute definition", Guid.Empty);
		}
	}
}
