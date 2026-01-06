using Application.Behaviors;
using Application.DTOs;
using MediatR;

namespace Application.Commands.Product.CreateProduct;

public sealed record CreateProductCommand(
	Guid UserId,
	string Name,
	string? Description,
	List<Guid> CategoryIds,
	decimal Price,
	int StockQuantity,
	Dictionary<string, object?>? Attributes = null,
	List<Guid>? TagIds = null
) : IRequest<ServiceResponse<Guid>>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["products"];

	public CreateProductCommand(
		Guid UserId,
		string Name,
		string? Description,
		Guid CategoryId,
		decimal Price,
		int StockQuantity,
		Dictionary<string, object?>? Attributes = null,
		List<Guid>? TagIds = null)
		: this(UserId, Name, Description, new List<Guid> { CategoryId }, Price, StockQuantity, Attributes, TagIds)
	{
	}
}
