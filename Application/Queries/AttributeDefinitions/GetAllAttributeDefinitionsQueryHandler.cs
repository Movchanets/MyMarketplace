using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.AttributeDefinitions;

public sealed class GetAllAttributeDefinitionsQueryHandler
	: IRequestHandler<GetAllAttributeDefinitionsQuery, ServiceResponse<IReadOnlyList<AttributeDefinitionDto>>>
{
	private readonly IAttributeDefinitionRepository _repository;
	private readonly ILogger<GetAllAttributeDefinitionsQueryHandler> _logger;

	public GetAllAttributeDefinitionsQueryHandler(
		IAttributeDefinitionRepository repository,
		ILogger<GetAllAttributeDefinitionsQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<ServiceResponse<IReadOnlyList<AttributeDefinitionDto>>> Handle(
		GetAllAttributeDefinitionsQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Getting all attribute definitions, includeInactive: {IncludeInactive}", request.IncludeInactive);

		try
		{
			var definitions = await _repository.GetAllAsync(request.IncludeInactive);

			var dtos = definitions.Select(d => new AttributeDefinitionDto(
				d.Id,
				d.Code,
				d.Name,
				d.DataType,
				d.IsRequired,
				d.IsVariant,
				d.Description,
				d.Unit,
				d.DisplayOrder,
				d.GetAllowedValuesList(),
				d.IsActive
			)).ToList().AsReadOnly();

			_logger.LogInformation("Found {Count} attribute definitions", dtos.Count);
			return new ServiceResponse<IReadOnlyList<AttributeDefinitionDto>>(true, "Success", dtos);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting attribute definitions");
			return new ServiceResponse<IReadOnlyList<AttributeDefinitionDto>>(false, "Failed to retrieve attribute definitions", null);
		}
	}
}
