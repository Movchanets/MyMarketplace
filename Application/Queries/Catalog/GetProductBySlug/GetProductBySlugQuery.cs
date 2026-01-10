using Application.DTOs;
using MediatR;

namespace Application.Queries.Catalog.GetProductBySlug;

public sealed record GetProductBySlugQuery(string ProductSlug, string? SkuCode = null) : IRequest<ServiceResponse<ProductDetailsDto>>;
