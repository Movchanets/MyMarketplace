using Application.Commands.Cart.AddToCart;
using Application.Commands.Cart.ClearCart;
using Application.Commands.Cart.MergeGuestCart;
using Application.Commands.Cart.RemoveFromCart;
using Application.Commands.Cart.UpdateCartQuantity;
using Application.DTOs;
using Application.Queries.Cart.GetUserCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

/// <summary>
/// API for managing shopping cart
/// </summary>
[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly ILogger<CartController> _logger;

	public CartController(
		IMediator mediator,
		ILogger<CartController> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	/// <summary>
	/// Get the current user's cart
	/// </summary>
	[HttpGet]
	[Authorize]
	public async Task<IActionResult> GetCart()
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<CartDto>(false, "User not authenticated", null));
			}

			var query = new GetUserCartQuery(userId.Value);
			var result = await _mediator.Send(query);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving cart");
			return StatusCode(500, new ServiceResponse<CartDto>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Add an item to the cart
	/// </summary>
	[HttpPost("items")]
	[Authorize]
	public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<CartDto>(false, "User not authenticated", null));
			}

			if (!ModelState.IsValid)
			{
				return BadRequest(new ServiceResponse<CartDto>(false, "Invalid request", null));
			}

			var command = new AddToCartCommand(
				userId.Value,
				request.ProductId,
				request.SkuId,
				request.Quantity);

			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error adding item to cart");
			return StatusCode(500, new ServiceResponse<CartDto>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Update the quantity of a cart item
	/// </summary>
	[HttpPut("items/{cartItemId:guid}")]
	[Authorize]
	public async Task<IActionResult> UpdateQuantity(Guid cartItemId, [FromBody] UpdateQuantityRequest request)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<CartDto>(false, "User not authenticated", null));
			}

			if (!ModelState.IsValid)
			{
				return BadRequest(new ServiceResponse<CartDto>(false, "Invalid request", null));
			}

			var command = new UpdateCartQuantityCommand(
				userId.Value,
				cartItemId,
				request.Quantity);

			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating cart quantity");
			return StatusCode(500, new ServiceResponse<CartDto>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Update quantity by SKU ID
	/// </summary>
	[HttpPut("items/sku/{skuId:guid}")]
	[Authorize]
	public async Task<IActionResult> UpdateQuantityBySku(Guid skuId, [FromBody] UpdateQuantityRequest request)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<CartDto>(false, "User not authenticated", null));
			}

			if (!ModelState.IsValid)
			{
				return BadRequest(new ServiceResponse<CartDto>(false, "Invalid request", null));
			}

			var command = new UpdateCartQuantityBySkuCommand(
				userId.Value,
				skuId,
				request.Quantity);

			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating cart quantity by SKU");
			return StatusCode(500, new ServiceResponse<CartDto>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Remove an item from the cart
	/// </summary>
	[HttpDelete("items/{cartItemId:guid}")]
	[Authorize]
	public async Task<IActionResult> RemoveFromCart(Guid cartItemId)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<CartDto>(false, "User not authenticated", null));
			}

			var command = new RemoveFromCartCommand(userId.Value, cartItemId);
			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error removing item from cart");
			return StatusCode(500, new ServiceResponse<CartDto>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Remove item from cart by SKU ID
	/// </summary>
	[HttpDelete("items/sku/{skuId:guid}")]
	[Authorize]
	public async Task<IActionResult> RemoveFromCartBySku(Guid skuId)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<CartDto>(false, "User not authenticated", null));
			}

			var command = new RemoveFromCartBySkuCommand(userId.Value, skuId);
			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error removing item from cart by SKU");
			return StatusCode(500, new ServiceResponse<CartDto>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Clear all items from the cart
	/// </summary>
	[HttpDelete]
	[Authorize]
	public async Task<IActionResult> ClearCart()
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<bool>(false, "User not authenticated", false));
			}

			var command = new ClearCartCommand(userId.Value);
			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error clearing cart");
			return StatusCode(500, new ServiceResponse<bool>(false, "Internal server error", false));
		}
	}

	/// <summary>
	/// Merge guest cart items into the authenticated user's cart
	/// </summary>
	[HttpPost("merge")]
	[Authorize]
	public async Task<IActionResult> MergeGuestCart([FromBody] MergeGuestCartRequest request)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<CartDto>(false, "User not authenticated", null));
			}

			if (!ModelState.IsValid || request.Items == null || request.Items.Count == 0)
			{
				return BadRequest(new ServiceResponse<CartDto>(false, "Invalid request", null));
			}

			var command = new MergeGuestCartCommand(
				userId.Value,
				request.Items.Select(i => new MergeCartItemDto(i.ProductId, i.SkuId, i.Quantity)).ToList()
			);

			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error merging guest cart");
			return StatusCode(500, new ServiceResponse<CartDto>(false, "Internal server error", null));
		}
	}

	private Guid? GetUserId()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
		{
			return null;
		}
		return userId;
	}
}

/// <summary>
/// Request model for adding item to cart
/// </summary>
public class AddToCartRequest
{
	public Guid ProductId { get; set; }
	public Guid SkuId { get; set; }
	public int Quantity { get; set; }
}

/// <summary>
/// Request model for updating quantity
/// </summary>
public class UpdateQuantityRequest
{
	public int Quantity { get; set; }
}

/// <summary>
/// Request model for merging guest cart
/// </summary>
public class MergeGuestCartRequest
{
	public List<MergeGuestCartItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Individual item in a merge guest cart request
/// </summary>
public class MergeGuestCartItemRequest
{
	public Guid ProductId { get; set; }
	public Guid SkuId { get; set; }
	public int Quantity { get; set; }
}
