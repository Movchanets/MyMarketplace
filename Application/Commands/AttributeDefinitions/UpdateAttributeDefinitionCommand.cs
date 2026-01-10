using Application.DTOs;
using MediatR;

namespace Application.Commands.AttributeDefinitions;

public sealed record UpdateAttributeDefinitionCommand(
	Guid Id,
	string Name,
	string DataType,
	bool IsRequired,
	bool IsVariant,
	string? Description,
	string? Unit,
	int DisplayOrder,
	List<string>? AllowedValues
) : IRequest<ServiceResponse>;
