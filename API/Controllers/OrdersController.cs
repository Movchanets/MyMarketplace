using Application.Commands.Order.CancelOrder;
using Application.Commands.Order.CreateOrder;
using Application.Commands.Order.UpdateOrderStatus;
using Application.DTOs;
using Application.Queries.Order.GetOrderById;
using Application.Queries.Order.GetOrderStatusHistory;
using Application.Queries.Order.GetUserOrders;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

/// <summary>
/// API for managing orders
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly ILogger<OrdersController> _logger;

	public OrdersController(
		IMediator mediator,
		ILogger<OrdersController> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	/// <summary>
	/// Get user's orders with filtering, sorting, and pagination
	/// </summary>
	[HttpGet]
	[Authorize]
	public async Task<IActionResult> GetOrders(
		[FromQuery] OrderStatus? status = null,
		[FromQuery] DateTime? fromDate = null,
		[FromQuery] DateTime? toDate = null,
		[FromQuery] string? sortBy = null,
		[FromQuery] bool sortDescending = true,
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 20)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<PagedOrdersResult>(false, "User not authenticated", null));
			}

			var query = new GetUserOrdersQuery(
				userId.Value,
				status,
				fromDate,
				toDate,
				sortBy,
				sortDescending,
				pageNumber,
				pageSize);

			var result = await _mediator.Send(query);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving orders");
			return StatusCode(500, new ServiceResponse<PagedOrdersResult>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Get a specific order by ID
	/// </summary>
	[HttpGet("{orderId:guid}")]
	[Authorize]
	public async Task<IActionResult> GetOrder(Guid orderId)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<OrderDetailDto>(false, "User not authenticated", null));
			}

			var query = new GetOrderByIdQuery(orderId, userId.Value);
			var result = await _mediator.Send(query);

			if (!result.IsSuccess)
			{
				if (result.Message == "Order not found")
				{
					return NotFound(result);
				}
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
			return StatusCode(500, new ServiceResponse<OrderDetailDto>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Create a new order from the cart
	/// </summary>
	[HttpPost]
	[Authorize]
	public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<OrderDto>(false, "User not authenticated", null));
			}

			if (!ModelState.IsValid)
			{
				return BadRequest(new ServiceResponse<OrderDto>(false, "Invalid request", null));
			}

			// Build shipping address from request
			var shippingAddress = new ShippingAddress(
				request.ShippingAddress.FirstName,
				request.ShippingAddress.LastName,
				request.ShippingAddress.PhoneNumber,
				request.ShippingAddress.Email,
				request.ShippingAddress.AddressLine1,
				request.ShippingAddress.AddressLine2,
				request.ShippingAddress.City,
				request.ShippingAddress.State,
				request.ShippingAddress.PostalCode,
				request.ShippingAddress.Country);

			// Generate idempotency key if not provided
			var idempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString();

			var command = new CreateOrderCommand(
				userId.Value,
				shippingAddress,
				request.DeliveryMethod,
				request.PaymentMethod,
				request.PromoCode,
				request.CustomerNotes,
				idempotencyKey);

			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				return BadRequest(result);
			}

			return CreatedAtAction(nameof(GetOrder), new { orderId = result.Payload?.Id }, result);
		}
		catch (ArgumentException ex)
		{
			_logger.LogWarning(ex, "Invalid shipping address provided");
			return BadRequest(new ServiceResponse<OrderDto>(false, ex.Message, null));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating order");
			return StatusCode(500, new ServiceResponse<OrderDto>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Cancel an order
	/// </summary>
	[HttpPost("{orderId:guid}/cancel")]
	[Authorize]
	public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderRequest? request)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<bool>(false, "User not authenticated", false));
			}

			var command = new CancelOrderCommand(orderId, userId.Value, request?.Reason);
			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
				{
					return NotFound(result);
				}
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
			return StatusCode(500, new ServiceResponse<bool>(false, "Internal server error", false));
		}
	}

	/// <summary>
	/// Get order status history
	/// </summary>
	[HttpGet("{orderId:guid}/status-history")]
	[Authorize]
	public async Task<IActionResult> GetOrderStatusHistory(Guid orderId)
	{
		try
		{
			var userId = GetUserId();
			if (!userId.HasValue)
			{
				return Unauthorized(new ServiceResponse<OrderStatusHistoryResult>(false, "User not authenticated", null));
			}

			var query = new GetOrderStatusHistoryQuery(orderId, userId.Value);
			var result = await _mediator.Send(query);

			if (!result.IsSuccess)
			{
				if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
				{
					return NotFound(result);
				}
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving order status history for {OrderId}", orderId);
			return StatusCode(500, new ServiceResponse<OrderStatusHistoryResult>(false, "Internal server error", null));
		}
	}

	/// <summary>
	/// Update order status (Admin only)
	/// </summary>
	[HttpPut("{orderId:guid}/status")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
	{
		try
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(new ServiceResponse<OrderStatusDto>(false, "Invalid request", null));
			}

			var command = new UpdateOrderStatusCommand(
				orderId,
				request.NewStatus,
				request.TrackingNumber,
				request.ShippingCarrier);

			var result = await _mediator.Send(command);

			if (!result.IsSuccess)
			{
				if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
				{
					return NotFound(result);
				}
				return BadRequest(result);
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating order status for {OrderId}", orderId);
			return StatusCode(500, new ServiceResponse<OrderStatusDto>(false, "Internal server error", null));
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
/// Request model for creating an order
/// </summary>
public class CreateOrderRequest
{
	public ShippingAddressRequest ShippingAddress { get; set; } = null!;
	public string DeliveryMethod { get; set; } = string.Empty;
	public string PaymentMethod { get; set; } = string.Empty;
	public string? PromoCode { get; set; }
	public string? CustomerNotes { get; set; }
	public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Request model for shipping address
/// </summary>
public class ShippingAddressRequest
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string AddressLine1 { get; set; } = string.Empty;
	public string? AddressLine2 { get; set; }
	public string City { get; set; } = string.Empty;
	public string? State { get; set; }
	public string PostalCode { get; set; } = string.Empty;
	public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Request model for cancelling an order
/// </summary>
public class CancelOrderRequest
{
	public string? Reason { get; set; }
}

/// <summary>
/// Request model for updating order status
/// </summary>
public class UpdateOrderStatusRequest
{
	public OrderStatus NewStatus { get; set; }
	public string? TrackingNumber { get; set; }
	public string? ShippingCarrier { get; set; }
}
