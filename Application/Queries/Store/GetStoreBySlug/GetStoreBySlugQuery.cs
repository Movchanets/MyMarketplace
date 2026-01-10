using Application.DTOs;
using MediatR;

namespace Application.Queries.Store.GetStoreBySlug;

public sealed record GetStoreBySlugQuery(string Slug) : IRequest<ServiceResponse<PublicStoreDto?>>;

public sealed record PublicStoreDto(
	Guid Id,
	string Name,
	string Slug,
	string? Description,
	bool IsVerified,
	DateTime CreatedAt,
	IReadOnlyList<ProductSummaryDto> Products
);
