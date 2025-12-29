using Application.DTOs;
using MediatR;

namespace Application.Commands.Product.Sku.UpdateSku;

public sealed record UpdateSkuCommand(
	Guid UserId,
	Guid ProductId,
	Guid SkuId,
	decimal Price,
	int StockQuantity,
	IDictionary<string, object?>? Attributes = null
) : IRequest<ServiceResponse>;
