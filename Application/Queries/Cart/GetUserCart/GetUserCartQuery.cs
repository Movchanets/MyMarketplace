using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using MediatR;

namespace Application.Queries.Cart.GetUserCart;

/// <summary>
/// Query to get the user's cart with product details
/// </summary>
public sealed record GetUserCartQuery(
	Guid UserId
) : IRequest<ServiceResponse<CartDto>>;
