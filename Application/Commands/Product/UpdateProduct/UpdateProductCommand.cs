using Application.DTOs;
using MediatR;

namespace Application.Commands.Product.UpdateProduct;

public sealed record UpdateProductCommand(
	Guid UserId,
	Guid ProductId,
	string Name,
	string? Description,
	List<Guid> CategoryIds,
	List<Guid>? TagIds = null,
	Guid? PrimaryCategoryId = null
) : IRequest<ServiceResponse>
{
	public UpdateProductCommand(
		Guid UserId,
		Guid ProductId,
		string Name,
		string? Description,
		Guid CategoryId,
		List<Guid>? TagIds = null)
		: this(UserId, ProductId, Name, Description, new List<Guid> { CategoryId }, TagIds)
	{
	}
}
