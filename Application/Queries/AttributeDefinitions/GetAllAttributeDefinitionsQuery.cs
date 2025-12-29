using Application.DTOs;
using MediatR;

namespace Application.Queries.AttributeDefinitions;

public sealed record GetAllAttributeDefinitionsQuery(bool IncludeInactive = false)
	: IRequest<ServiceResponse<IReadOnlyList<AttributeDefinitionDto>>>;
