using Application.DTOs;
using MediatR;

namespace Application.Commands.AttributeDefinitions;

public sealed record CreateAttributeDefinitionCommand(
	string Code,
	string Name,
	string DataType = "string",
	bool IsRequired = false,
	bool IsVariant = false,
	string? Description = null,
	string? Unit = null,
	int DisplayOrder = 0,
	List<string>? AllowedValues = null
) : IRequest<ServiceResponse<Guid>>;
