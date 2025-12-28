using Application.DTOs;
using MediatR;

namespace Application.Queries.Catalog.GetMyProducts;

/// <summary>
/// Query для отримання продуктів поточного продавця (по userId)
/// </summary>
public sealed record GetMyProductsQuery(Guid UserId) : IRequest<ServiceResponse<IReadOnlyList<ProductSummaryDto>>>;
