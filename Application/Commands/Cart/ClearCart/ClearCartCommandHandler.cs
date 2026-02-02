using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Cart.ClearCart;

public sealed class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, ServiceResponse<bool>>
{
	private readonly ICartService _cartService;
	private readonly ICartRepository _cartRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<ClearCartCommandHandler> _logger;

	public ClearCartCommandHandler(
		ICartService cartService,
		ICartRepository cartRepository,
		IUnitOfWork unitOfWork,
		ILogger<ClearCartCommandHandler> logger)
	{
		_cartService = cartService;
		_cartRepository = cartRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<bool>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Clearing cart for user {UserId}", request.UserId);

		try
		{
			// Get cart - use GetCartAsync for optional cart scenario
			var cartResult = await _cartService.GetCartAsync(request.UserId, cancellationToken);

			// If cart not found, it's already empty - return success
			if (!cartResult.IsSuccess)
			{
				// Check if it's a "User not found" error
				if (cartResult.ErrorMessage == "User not found")
				{
					return new ServiceResponse<bool>(false, "User not found", false);
				}

				// Cart not found means already empty
				_logger.LogInformation("Cart not found for user {UserId}, nothing to clear", request.UserId);
				return new ServiceResponse<bool>(true, "Cart is already empty", true);
			}

			var (cart, _) = cartResult.Data;

			// Check if cart is already empty
			if (!cart.Items.Any())
			{
				_logger.LogInformation("Cart is already empty for user {UserId}", request.UserId);
				return new ServiceResponse<bool>(true, "Cart is already empty", true);
			}

			// Clear cart (EF Core change tracking automatically detects modifications)
			cart.Clear();

			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Cleared cart for user {UserId}", request.UserId);
			return new ServiceResponse<bool>(true, "Cart cleared successfully", true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error clearing cart for user {UserId}", request.UserId);
			return new ServiceResponse<bool>(false, "An error occurred while clearing cart", false);
		}
	}
}
