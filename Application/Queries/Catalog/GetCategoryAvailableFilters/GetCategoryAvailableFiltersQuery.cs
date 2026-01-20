using Application.DTOs;
using MediatR;

namespace Application.Queries.Catalog.GetCategoryAvailableFilters;

public sealed record GetCategoryAvailableFiltersQuery(Guid CategoryId)
	: IRequest<ServiceResponse<CategoryAvailableFiltersDto>>;
