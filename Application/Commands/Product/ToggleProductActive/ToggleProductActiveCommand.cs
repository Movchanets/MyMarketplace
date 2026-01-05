using Application.DTOs;
using MediatR;

namespace Application.Commands.Product.ToggleProductActive;

public sealed record ToggleProductActiveCommand(
	Guid UserId,
	Guid ProductId,
	bool IsActive
) : IRequest<ServiceResponse>;
