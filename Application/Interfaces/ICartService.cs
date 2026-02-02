using Application.Commands.Cart.AddToCart;
using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Service interface for cart operations providing common cart functionality with ACID support
/// </summary>
public interface ICartService
{
	/// <summary>
	/// Gets or creates a cart for the specified user
	/// </summary>
	/// <param name="userId">Identity user ID (not domain user ID)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the cart and domain user, or error message</returns>
	Task<CartOperationResult<(Cart Cart, User DomainUser, bool IsNewCart)>> GetOrCreateCartAsync(
		Guid userId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an existing cart for the specified user
	/// </summary>
	/// <param name="userId">Identity user ID (not domain user ID)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the cart and domain user, or error message</returns>
	Task<CartOperationResult<(Cart Cart, User DomainUser)>> GetCartAsync(
		Guid userId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets an existing cart with products and SKU details loaded (for checkout/display)
	/// </summary>
	/// <param name="userId">Identity user ID (not domain user ID)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the cart with products and domain user, or error message</returns>
	Task<CartOperationResult<(Cart Cart, User DomainUser)>> GetCartWithProductsAsync(
		Guid userId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Validates quantity constraints
	/// </summary>
	/// <param name="quantity">The quantity to validate</param>
	/// <returns>Validation result with error message if invalid</returns>
	CartOperationResult<bool> ValidateQuantity(int quantity);

	/// <summary>
	/// Validates quantity constraints with existing cart quantity for a SKU
	/// </summary>
	/// <param name="requestedQuantity">The requested quantity to add</param>
	/// <param name="existingQuantity">The existing quantity in cart</param>
	/// <returns>Validation result with error message if invalid</returns>
	CartOperationResult<int> ValidateTotalQuantity(int requestedQuantity, int existingQuantity);

	/// <summary>
	/// Maps a Cart entity to CartDto
	/// </summary>
	/// <param name="cart">The cart entity</param>
	/// <returns>The mapped CartDto</returns>
	CartDto MapToCartDto(Cart cart);

	/// <summary>
	/// Adds or updates a cart item for the given user in a transactional and concurrency-safe manner
	/// </summary>
	Task<ServiceResponse<CartDto>> AddOrUpdateItemAsync(Guid userId, Guid productId, Guid skuId, int quantity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result type for cart operations supporting success/failure with typed data
/// </summary>
/// <typeparam name="T">The type of data on success</typeparam>
public sealed record CartOperationResult<T>
{
	public bool IsSuccess { get; init; }
	public string? ErrorMessage { get; init; }
	public T? Data { get; init; }

	private CartOperationResult() { }

	public static CartOperationResult<T> Success(T data) => new()
	{
		IsSuccess = true,
		Data = data
	};

	public static CartOperationResult<T> Failure(string errorMessage) => new()
	{
		IsSuccess = false,
		ErrorMessage = errorMessage
	};

	/// <summary>
	/// Converts to ServiceResponse
	/// </summary>
	public ServiceResponse<TDto> ToServiceResponse<TDto>(TDto? dto, string? successMessage = null) =>
		IsSuccess
			? new ServiceResponse<TDto>(true, successMessage ?? "Operation successful", dto)
			: new ServiceResponse<TDto>(false, ErrorMessage ?? "Operation failed", default);

	/// <summary>
	/// Converts failure to ServiceResponse with different generic type
	/// </summary>
	public ServiceResponse<TDto> ToFailureResponse<TDto>() =>
		new(false, ErrorMessage ?? "Operation failed", default);
}
