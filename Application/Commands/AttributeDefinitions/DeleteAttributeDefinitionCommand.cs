using Application.DTOs;
using MediatR;

namespace Application.Commands.AttributeDefinitions;

public sealed record DeleteAttributeDefinitionCommand(Guid Id) : IRequest<ServiceResponse>;
