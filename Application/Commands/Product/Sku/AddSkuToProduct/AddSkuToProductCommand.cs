using Application.DTOs;
using MediatR;

namespace Application.Commands.Product.Sku.AddSkuToProduct;

public sealed record AddSkuToProductCommand(
	Guid UserId,
	Guid ProductId,
	decimal Price,
	int StockQuantity,
	Dictionary<string, object?>? Attributes = null
) : IRequest<ServiceResponse<string>>;
