using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Order.GetUserOrders;

public sealed class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, ServiceResponse<PagedOrdersResult>>
{
	private readonly IOrderRepository _orderRepository;
	private readonly IUserRepository _userRepository;
	private readonly ILogger<GetUserOrdersQueryHandler> _logger;

	public GetUserOrdersQueryHandler(
		IOrderRepository orderRepository,
		IUserRepository userRepository,
		ILogger<GetUserOrdersQueryHandler> logger)
	{
		_orderRepository = orderRepository;
		_userRepository = userRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<PagedOrdersResult>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Getting orders for user {UserId}, page {Page}",
			request.UserId, request.PageNumber);

		try
		{
			// Validate user
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<PagedOrdersResult>(false, "User not found", null);
			}

			// Get orders with pagination
			var (orders, totalCount) = await _orderRepository.GetByUserIdAsync(
				domainUser.Id,
				request.Status,
				request.FromDate,
				request.ToDate,
				request.SortBy,
				request.SortDescending,
				request.PageNumber,
				request.PageSize,
				cancellationToken);

			// Map to DTOs
			var orderDtos = orders.Select(order => new OrderSummaryDto(
				order.Id,
				order.OrderNumber,
				order.Status,
				order.PaymentStatus,
				order.TotalPrice,
				order.GetTotalItems(),
				order.CreatedAt,
				order.TrackingNumber,
				order.ShippingCarrier
			)).ToList();

			var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

			var result = new PagedOrdersResult(
				orderDtos,
				totalCount,
				request.PageNumber,
				request.PageSize,
				totalPages
			);

			return new ServiceResponse<PagedOrdersResult>(true, "Orders retrieved successfully", result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting orders for user {UserId}", request.UserId);
			return new ServiceResponse<PagedOrdersResult>(false, "An error occurred while retrieving orders", null);
		}
	}
}
