using Application.DTOs;
using MediatR;

namespace Application.Queries.Catalog.FilterProducts;

public sealed record FilterProductsQuery(
	Guid? CategoryId = null,
	List<Guid>? TagIds = null,
	decimal? MinPrice = null,
	decimal? MaxPrice = null,
	bool? InStock = null,
	Dictionary<string, AttributeFilterValue>? Attributes = null,
	ProductSort Sort = ProductSort.Relevance,
	int Page = 1,
	int PageSize = 24
) : IRequest<ServiceResponse<PagedResponse<ProductSummaryDto>>>;
