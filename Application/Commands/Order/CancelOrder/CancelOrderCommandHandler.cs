using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Order.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, ServiceResponse<bool>>
{
	private readonly IOrderRepository _orderRepository;
	private readonly ISkuRepository _skuRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<CancelOrderCommandHandler> _logger;

	public CancelOrderCommandHandler(
		IOrderRepository orderRepository,
		ISkuRepository skuRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<CancelOrderCommandHandler> logger)
	{
		_orderRepository = orderRepository;
		_skuRepository = skuRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Cancelling order {OrderId} for user {UserId}",
			request.OrderId, request.UserId);

		await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

		try
		{
			// Validate user
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<bool>(false, "User not found", false);
			}

			// Get order
			var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
			if (order is null)
			{
				_logger.LogWarning("Order {OrderId} not found", request.OrderId);
				return new ServiceResponse<bool>(false, "Order not found", false);
			}

			// Verify order belongs to user
			if (order.UserId != domainUser.Id)
			{
				_logger.LogWarning("Order {OrderId} does not belong to user {UserId}",
					request.OrderId, request.UserId);
				return new ServiceResponse<bool>(false, "Order not found", false);
			}

			// Check if order can be cancelled
			if (!order.Status.CanCancel())
			{
				_logger.LogWarning("Order {OrderId} with status {Status} cannot be cancelled",
					request.OrderId, order.Status);
				return new ServiceResponse<bool>(false,
					$"Order with status '{order.Status.GetDisplayName()}' cannot be cancelled", false);
			}

			// Restore inventory for all items
			foreach (var orderItem in order.Items)
			{
				var sku = await _skuRepository.GetByIdAsync(orderItem.SkuId);
				if (sku is not null)
				{
					sku.RestoreStock(orderItem.Quantity);
					_skuRepository.Update(sku);

					_logger.LogInformation("Restored {Quantity} stock for SKU {SkuId} from cancelled order {OrderId}",
						orderItem.Quantity, orderItem.SkuId, request.OrderId);
				}
			}

			// Cancel order
			order.Cancel(request.Reason);
			_orderRepository.Update(order);

			await _unitOfWork.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			_logger.LogInformation("Order {OrderId} cancelled successfully for user {UserId}",
				request.OrderId, request.UserId);

			return new ServiceResponse<bool>(true, "Order cancelled successfully", true);
		}
		catch (InvalidOperationException ex)
		{
			await transaction.RollbackAsync(cancellationToken);
			_logger.LogWarning(ex, "Invalid operation cancelling order {OrderId}", request.OrderId);
			return new ServiceResponse<bool>(false, ex.Message, false);
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync(cancellationToken);
			_logger.LogError(ex, "Error cancelling order {OrderId}", request.OrderId);
			return new ServiceResponse<bool>(false, "An error occurred while cancelling order", false);
		}
	}
}
